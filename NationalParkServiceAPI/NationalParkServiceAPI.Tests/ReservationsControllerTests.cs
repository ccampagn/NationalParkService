using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalParkServiceAPI.Controllers;
using NationalParkServiceAPI.Data;
using NationalParkServiceAPI.Models;
using Xunit;

namespace NationalParkServiceAPI.Tests;

public class ReservationsControllerTests
{
    private static readonly DateOnly Today = new(2026, 10, 19);
    private const int Ada = 1;
    private const int Grace = 2;
    private const int TentSite = 100;
    private const int GroupSite = 101;
    private const int SeasonalSite = 200;

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static NationalParkServiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NationalParkServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new NationalParkServiceDbContext(options);
        context.Parks.Add(new Park { ParkId = 1, ParkName = "Shenandoah" });
        context.Campgrounds.Add(new Campground { CampgroundId = 10, ParkId = 1, CampgroundName = "Big Meadows", NightlyRate = 30, MaxGuests = 6 });
        context.Users.Add(new User { UserId = Ada, FirstName = "Ada", Email = "ada@example.com" });
        context.Users.Add(new User { UserId = Grace, FirstName = "Grace", Email = "grace@example.com" });
        context.Sites.Add(new Site { SiteId = TentSite, CampgroundId = 10, SiteNumber = "01", SiteType = "Tent", MaxGuests = 6, NightlyRate = 30 });
        context.Sites.Add(new Site { SiteId = GroupSite, CampgroundId = 10, SiteNumber = "10", SiteType = "Group", MaxGuests = 12, NightlyRate = 60 });
        // Loft Mountain's last night of the season is Oct 31, 12 days after Today.
        context.Campgrounds.Add(new Campground { CampgroundId = 20, ParkId = 1, CampgroundName = "Loft Mountain", NightlyRate = 30, MaxGuests = 6, SeasonOpens = "05-08", SeasonCloses = "10-31" });
        context.Sites.Add(new Site { SiteId = SeasonalSite, CampgroundId = 20, SiteNumber = "01", SiteType = "Tent", MaxGuests = 6, NightlyRate = 30 });
        context.SaveChanges();
        return context;
    }

    private static ReservationsController CreateController(NationalParkServiceDbContext context, int userId = Ada) =>
        new ReservationsController(context, new FixedTimeProvider()).As(userId);

    [Fact]
    public async Task CreateReservation_SavesReservationAndCalculatesTotal()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.CreateReservation(new ReservationRequest(TentSite, Today.AddDays(7), Today.AddDays(10), 4));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var reservation = Assert.IsType<ReservationResponse>(created.Value);
        Assert.Matches("^NPS-[A-Z0-9]{6}$", reservation.Code);
        Assert.Equal("Shenandoah", reservation.ParkName);
        Assert.Equal("Big Meadows", reservation.Campground);
        Assert.Equal("01", reservation.SiteNumber);
        Assert.Equal("Tent", reservation.SiteType);
        Assert.Equal(3, reservation.Nights);
        Assert.Equal(3 * 30 + ReservationsController.ReservationFee, reservation.Total);
        Assert.Equal(new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc), reservation.BookedAt);
        Assert.Equal(DateTimeKind.Utc, reservation.BookedAt.Kind);

        var saved = Assert.Single(context.Reservations);
        Assert.Equal(reservation.Code, saved.ConfirmationCode);
        Assert.Equal(4, saved.Guests);
        Assert.Equal(Ada, saved.UserId);
        Assert.Equal(TentSite, saved.SiteId);
        Assert.Equal(10, saved.CampgroundId);
    }

    [Fact]
    public async Task CreateReservation_UsesTheSitesRateAndGuestLimit()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.CreateReservation(new ReservationRequest(GroupSite, Today.AddDays(7), Today.AddDays(9), 10));

        var reservation = Assert.IsType<ReservationResponse>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
        Assert.Equal(60, reservation.NightlyRate);
        Assert.Equal(2 * 60 + ReservationsController.ReservationFee, reservation.Total);
    }

    [Theory]
    [InlineData(7, 10)]  // same nights
    [InlineData(8, 9)]   // inside the existing stay
    [InlineData(5, 8)]   // overlaps the first night
    [InlineData(9, 12)]  // overlaps the last night
    public async Task CreateReservation_ReturnsConflict_WhenTheSiteIsAlreadyBooked(int checkInDays, int checkOutDays)
    {
        using var context = CreateContext();
        await CreateController(context, Ada).CreateReservation(new ReservationRequest(TentSite, Today.AddDays(7), Today.AddDays(10), 2));

        var result = await CreateController(context, Grace).CreateReservation(
            new ReservationRequest(TentSite, Today.AddDays(checkInDays), Today.AddDays(checkOutDays), 2));

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Single(context.Reservations);
    }

    [Fact]
    public async Task CreateReservation_AllowsCheckingInTheDayAnotherGuestChecksOut()
    {
        using var context = CreateContext();
        await CreateController(context, Ada).CreateReservation(new ReservationRequest(TentSite, Today.AddDays(7), Today.AddDays(10), 2));

        var result = await CreateController(context, Grace).CreateReservation(
            new ReservationRequest(TentSite, Today.AddDays(10), Today.AddDays(12), 2));

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task CreateReservation_AllowsTheLastNightOfTheSeason()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.CreateReservation(new ReservationRequest(SeasonalSite, Today.AddDays(11), Today.AddDays(13), 2));

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Theory]
    [InlineData(99, 7, 10, 2)]   // site does not exist
    [InlineData(TentSite, -1, 2, 2)]   // check-in in the past
    [InlineData(TentSite, 7, 7, 2)]    // zero nights
    [InlineData(TentSite, 7, 22, 2)]   // more than 14 nights
    [InlineData(TentSite, 7, 10, 7)]   // more guests than the site allows
    [InlineData(TentSite, 7, 10, 0)]   // no guests
    [InlineData(SeasonalSite, 12, 14, 2)]   // last night falls after the campground closes for the season
    [InlineData(SeasonalSite, 20, 22, 2)]   // campground closed for the season
    public async Task CreateReservation_ReturnsBadRequest_ForInvalidRequests(int siteId, int checkInDays, int checkOutDays, int guests)
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.CreateReservation(
            new ReservationRequest(siteId, Today.AddDays(checkInDays), Today.AddDays(checkOutDays), guests));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task GetReservations_ReturnsSavedReservations()
    {
        using var context = CreateContext();
        var controller = CreateController(context);
        await controller.CreateReservation(new ReservationRequest(TentSite, Today.AddDays(1), Today.AddDays(2), 2));
        await controller.CreateReservation(new ReservationRequest(TentSite, Today.AddDays(5), Today.AddDays(8), 3));

        var result = await controller.GetReservations();

        var reservations = Assert.IsAssignableFrom<IEnumerable<ReservationResponse>>(result.Value);
        Assert.Equal(2, reservations.Count());
        Assert.All(reservations, r => Assert.Equal("Big Meadows", r.Campground));
    }

    [Fact]
    public async Task GetReservations_OnlyReturnsTheSignedInUsersReservations()
    {
        using var context = CreateContext();
        await CreateController(context, Ada).CreateReservation(new ReservationRequest(TentSite, Today.AddDays(1), Today.AddDays(2), 2));
        await CreateController(context, Grace).CreateReservation(new ReservationRequest(TentSite, Today.AddDays(5), Today.AddDays(8), 3));
        context.Reservations.Add(new Reservation { ConfirmationCode = "NPS-OLD000", CampgroundId = 10, UserId = null });
        context.SaveChanges();

        var result = await CreateController(context, Grace).GetReservations();

        var reservation = Assert.Single(result.Value!);
        Assert.Equal(3, reservation.Guests);
    }

    [Fact]
    public async Task GetReservation_ReturnsNotFound_ForAnotherUsersCode()
    {
        using var context = CreateContext();
        var created = await CreateController(context, Ada).CreateReservation(new ReservationRequest(TentSite, Today.AddDays(1), Today.AddDays(2), 2));
        var code = ((ReservationResponse)((CreatedAtActionResult)created.Result!).Value!).Code;

        var result = await CreateController(context, Grace).GetReservation(code);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CancelReservation_ReturnsNotFound_ForAnotherUsersCode()
    {
        using var context = CreateContext();
        var created = await CreateController(context, Ada).CreateReservation(new ReservationRequest(TentSite, Today.AddDays(1), Today.AddDays(2), 2));
        var code = ((ReservationResponse)((CreatedAtActionResult)created.Result!).Value!).Code;

        var result = await CreateController(context, Grace).CancelReservation(code);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Single(context.Reservations);
    }

    [Fact]
    public async Task GetReservation_ReturnsNotFound_ForUnknownCode()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetReservation("NPS-NOPE00");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CancelReservation_RemovesReservation()
    {
        using var context = CreateContext();
        var controller = CreateController(context);
        var created = await controller.CreateReservation(new ReservationRequest(TentSite, Today.AddDays(1), Today.AddDays(2), 2));
        var code = ((ReservationResponse)((CreatedAtActionResult)created.Result!).Value!).Code;

        var result = await controller.CancelReservation(code);

        Assert.Equal(code, result.Value!.Code);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task CancelReservation_ReturnsNotFound_ForUnknownCode()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.CancelReservation("NPS-NOPE00");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static async Task<string> Book(NationalParkServiceDbContext context, int siteId, int checkInDays, int checkOutDays, int userId = Ada)
    {
        var created = await CreateController(context, userId).CreateReservation(
            new ReservationRequest(siteId, Today.AddDays(checkInDays), Today.AddDays(checkOutDays), 2));
        return ((ReservationResponse)((CreatedAtActionResult)created.Result!).Value!).Code;
    }

    [Fact]
    public async Task ChangeReservation_MovesTheDatesAndRecalculatesTotal()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 7, 10);

        var result = await CreateController(context).ChangeReservation(
            code, new ReservationChangeRequest(TentSite, Today.AddDays(8), Today.AddDays(13)));

        var reservation = Assert.IsType<ReservationResponse>(result.Value);
        Assert.Equal(code, reservation.Code);
        Assert.Equal(5, reservation.Nights);
        Assert.Equal(0, reservation.ChangeFees);
        Assert.Equal(5 * 30 + ReservationsController.ReservationFee, reservation.Total);
        var saved = Assert.Single(context.Reservations);
        Assert.Equal(Today.AddDays(8), saved.CheckIn);
        Assert.Equal(Today.AddDays(13), saved.CheckOut);
        Assert.Equal(TentSite, saved.SiteId);
    }

    [Fact]
    public async Task ChangeReservation_KeepsTheRateItWasBookedAt_OnTheSameSite()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 7, 10);
        context.Sites.Find(TentSite)!.NightlyRate = 45;
        context.SaveChanges();

        var result = await CreateController(context).ChangeReservation(
            code, new ReservationChangeRequest(TentSite, Today.AddDays(7), Today.AddDays(9)));

        Assert.Equal(2 * 30 + ReservationsController.ReservationFee, result.Value!.Total);
    }

    [Fact]
    public async Task ChangeReservation_ReturnsConflict_WhenTheNewNightsAreBooked()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 7, 10);
        await Book(context, TentSite, 12, 14, Grace);

        var result = await CreateController(context).ChangeReservation(
            code, new ReservationChangeRequest(TentSite, Today.AddDays(9), Today.AddDays(13)));

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(Today.AddDays(7), context.Reservations.Single(r => r.ConfirmationCode == code).CheckIn);
    }

    [Theory]
    [InlineData(TentSite, -1, 2)]       // check-in in the past
    [InlineData(TentSite, 7, 7)]        // zero nights
    [InlineData(TentSite, 7, 22)]       // more than 14 nights
    [InlineData(SeasonalSite, 11, 14)]  // last night falls after the campground closes for the season
    public async Task ChangeReservation_ReturnsBadRequest_ForInvalidChanges(int siteId, int checkInDays, int checkOutDays)
    {
        using var context = CreateContext();
        var code = await Book(context, siteId, 5, 6);

        var result = await CreateController(context).ChangeReservation(
            code, new ReservationChangeRequest(siteId, Today.AddDays(checkInDays), Today.AddDays(checkOutDays)));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(Today.AddDays(5), Assert.Single(context.Reservations).CheckIn);
    }

    [Fact]
    public async Task ChangeReservation_ReturnsBadRequest_OnceTheTripHasStarted()
    {
        using var context = CreateContext();
        context.Reservations.Add(new Reservation
        {
            ConfirmationCode = "NPS-STAY00", UserId = Ada, CampgroundId = 10, SiteId = TentSite,
            CheckIn = Today.AddDays(-1), CheckOut = Today.AddDays(2), Guests = 2, NightlyRate = 30,
        });
        context.SaveChanges();

        var result = await CreateController(context).ChangeReservation(
            "NPS-STAY00", new ReservationChangeRequest(TentSite, Today.AddDays(1), Today.AddDays(3)));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ChangeReservation_ReturnsNotFound_ForAnotherUsersCode()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 7, 10);

        var result = await CreateController(context, Grace).ChangeReservation(
            code, new ReservationChangeRequest(TentSite, Today.AddDays(8), Today.AddDays(9)));

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Equal(Today.AddDays(7), Assert.Single(context.Reservations).CheckIn);
    }

    [Fact]
    public async Task ChangeReservation_MovesToAnotherSiteAtItsRate()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 7, 10);

        var result = await CreateController(context).ChangeReservation(
            code, new ReservationChangeRequest(GroupSite, Today.AddDays(7), Today.AddDays(10)));

        var reservation = Assert.IsType<ReservationResponse>(result.Value);
        Assert.Equal(code, reservation.Code);
        Assert.Equal(GroupSite, reservation.SiteId);
        Assert.Equal("10", reservation.SiteNumber);
        Assert.Equal("Group", reservation.SiteType);
        Assert.Equal(60, reservation.NightlyRate);
        Assert.Equal(3 * 60 + ReservationsController.ReservationFee, reservation.Total);
        Assert.Equal(GroupSite, Assert.Single(context.Reservations).SiteId);
    }

    [Fact]
    public async Task ChangeReservation_ReturnsConflict_WhenTheNewSiteIsBooked()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 7, 10);
        await Book(context, GroupSite, 8, 9, Grace);

        var result = await CreateController(context).ChangeReservation(
            code, new ReservationChangeRequest(GroupSite, Today.AddDays(7), Today.AddDays(10)));

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(TentSite, context.Reservations.Single(r => r.ConfirmationCode == code).SiteId);
    }

    [Fact]
    public async Task ChangeReservation_ReturnsBadRequest_ForASiteInAnotherCampground()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 7, 10);

        var result = await CreateController(context).ChangeReservation(
            code, new ReservationChangeRequest(SeasonalSite, Today.AddDays(7), Today.AddDays(10)));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(TentSite, Assert.Single(context.Reservations).SiteId);
    }

    [Fact]
    public async Task ChangeReservation_ReturnsBadRequest_WhenTheGuestsDontFitTheNewSite()
    {
        using var context = CreateContext();
        var created = await CreateController(context).CreateReservation(
            new ReservationRequest(GroupSite, Today.AddDays(7), Today.AddDays(10), 10));
        var code = ((ReservationResponse)((CreatedAtActionResult)created.Result!).Value!).Code;

        var result = await CreateController(context).ChangeReservation(
            code, new ReservationChangeRequest(TentSite, Today.AddDays(7), Today.AddDays(10)));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(GroupSite, Assert.Single(context.Reservations).SiteId);
    }

    [Fact]
    public async Task ChangeReservation_ReturnsBadRequest_ForUnknownSite()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 7, 10);

        var result = await CreateController(context).ChangeReservation(
            code, new ReservationChangeRequest(99, Today.AddDays(7), Today.AddDays(10)));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ChangeReservation_ChargesAChangeFee_ForEntirelyNewDates()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 7, 10);

        var first = await CreateController(context).ChangeReservation(
            code, new ReservationChangeRequest(TentSite, Today.AddDays(10), Today.AddDays(12)));
        var second = await CreateController(context).ChangeReservation(
            code, new ReservationChangeRequest(TentSite, Today.AddDays(20), Today.AddDays(22)));

        Assert.Equal(ReservationsController.ChangeFee, first.Value!.ChangeFees);
        Assert.Equal(2 * ReservationsController.ChangeFee, second.Value!.ChangeFees);
        Assert.Equal(2 * 30 + ReservationsController.ReservationFee + 2 * ReservationsController.ChangeFee, second.Value.Total);
    }

    [Fact]
    public async Task ChangeReservation_IsFree_WhenOnlyTheSiteChanges()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 7, 10);

        var result = await CreateController(context).ChangeReservation(
            code, new ReservationChangeRequest(GroupSite, Today.AddDays(7), Today.AddDays(10)));

        Assert.Equal(0, result.Value!.ChangeFees);
    }

    [Fact]
    public async Task ChangeReservation_ReturnsBadRequest_OnTheCheckInDay()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 0, 2);

        var result = await CreateController(context).ChangeReservation(
            code, new ReservationChangeRequest(TentSite, Today.AddDays(1), Today.AddDays(3)));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CancelReservation_RefundsAllButTheReservationAndCancellationFees()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 7, 10);

        var result = await CreateController(context).CancelReservation(code);

        Assert.Equal(3 * 30 - ReservationsController.CancellationFee, result.Value!.Refund);
    }

    [Theory]
    [InlineData(1)]  // the day before arrival
    [InlineData(0)]  // the day of arrival
    public async Task CancelReservation_ForfeitsTheFirstNight_WhenLate(int checkInDays)
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, checkInDays, checkInDays + 3);

        var result = await CreateController(context).CancelReservation(code);

        Assert.Equal(2 * 30 - ReservationsController.CancellationFee, result.Value!.Refund);
    }

    [Fact]
    public async Task CancelReservation_RefundsNothing_ForALateOneNightStay()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 1, 2);

        var result = await CreateController(context).CancelReservation(code);

        Assert.Equal(0, result.Value!.Refund);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task CancelReservation_DoesNotRefundChangeFees()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 7, 10);
        await CreateController(context).ChangeReservation(code, new ReservationChangeRequest(TentSite, Today.AddDays(20), Today.AddDays(23)));

        var result = await CreateController(context).CancelReservation(code);

        Assert.Equal(3 * 30 - ReservationsController.CancellationFee, result.Value!.Refund);
    }

    [Fact]
    public async Task CancelReservation_ReturnsBadRequest_OnceTheTripHasStarted()
    {
        using var context = CreateContext();
        context.Reservations.Add(new Reservation
        {
            ConfirmationCode = "NPS-STAY00", UserId = Ada, CampgroundId = 10, SiteId = TentSite,
            CheckIn = Today.AddDays(-1), CheckOut = Today.AddDays(2), Guests = 2, NightlyRate = 30,
        });
        context.SaveChanges();

        var result = await CreateController(context).CancelReservation("NPS-STAY00");

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Single(context.Reservations);
    }

    [Fact]
    public async Task CreateReservation_ReturnsBadRequest_ForARetiredSite()
    {
        using var context = CreateContext();
        context.Sites.Find(TentSite)!.Retired = true;
        context.SaveChanges();

        var result = await CreateController(context).CreateReservation(
            new ReservationRequest(TentSite, Today.AddDays(7), Today.AddDays(10), 2));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task ChangeReservation_CanKeepItsOwnRetiredSite_ButNotMoveToAnother()
    {
        using var context = CreateContext();
        var code = await Book(context, TentSite, 7, 10);
        var other = await Book(context, GroupSite, 20, 22, Grace);
        context.Sites.Find(TentSite)!.Retired = true;
        context.Sites.Find(GroupSite)!.Retired = true;
        context.SaveChanges();

        var keep = await CreateController(context).ChangeReservation(
            code, new ReservationChangeRequest(TentSite, Today.AddDays(8), Today.AddDays(10)));
        var move = await CreateController(context, Grace).ChangeReservation(
            other, new ReservationChangeRequest(TentSite, Today.AddDays(20), Today.AddDays(22)));

        Assert.IsType<ReservationResponse>(keep.Value);
        Assert.IsType<BadRequestObjectResult>(move.Result);
    }
}
