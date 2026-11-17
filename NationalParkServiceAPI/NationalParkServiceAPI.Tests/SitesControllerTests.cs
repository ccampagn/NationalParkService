using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalParkServiceAPI.Controllers;
using NationalParkServiceAPI.Data;
using NationalParkServiceAPI.Models;
using Xunit;

namespace NationalParkServiceAPI.Tests;

public class SitesControllerTests
{
    private static readonly DateOnly Nov4 = new(2026, 11, 4);

    private static NationalParkServiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NationalParkServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new NationalParkServiceDbContext(options);
        context.Campgrounds.Add(new Campground { CampgroundId = 10, ParkId = 1, CampgroundName = "Big Meadows" });
        context.Campgrounds.Add(new Campground { CampgroundId = 20, ParkId = 1, CampgroundName = "Loft Mountain", SeasonOpens = "05-08", SeasonCloses = "10-31" });
        context.Sites.AddRange(
            new Site { SiteId = 102, CampgroundId = 10, SiteNumber = "02", SiteType = "Tent", MaxGuests = 6, NightlyRate = 30 },
            new Site { SiteId = 101, CampgroundId = 10, SiteNumber = "01", SiteType = "Tent", MaxGuests = 6, NightlyRate = 30 },
            new Site { SiteId = 201, CampgroundId = 20, SiteNumber = "01", SiteType = "RV", MaxGuests = 8, NightlyRate = 40 });
        // Site 01 is taken Nov 4-7 by user 1.
        context.Reservations.Add(new Reservation { ConfirmationCode = "NPS-AAAAAA", UserId = 1, CampgroundId = 10, SiteId = 101, CheckIn = Nov4, CheckOut = Nov4.AddDays(3) });
        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task GetSites_ListsTheCampgroundsSitesWithAvailability()
    {
        using var context = CreateContext();

        var result = await new SitesController(context).GetSites(10, Nov4.AddDays(1), Nov4.AddDays(2));

        var sites = result.Value!.ToList();
        Assert.Equal(new[] { "01", "02" }, sites.Select(s => s.SiteNumber));
        Assert.False(sites[0].Available);
        Assert.True(sites[1].Available);
    }

    [Fact]
    public async Task GetSites_TreatsCheckOutDayAsFree()
    {
        using var context = CreateContext();

        var result = await new SitesController(context).GetSites(10, Nov4.AddDays(3), Nov4.AddDays(5));

        Assert.All(result.Value!, s => Assert.True(s.Available));
    }

    [Fact]
    public async Task GetSites_ReturnsNotFound_ForUnknownCampground()
    {
        using var context = CreateContext();

        var result = await new SitesController(context).GetSites(99, Nov4, Nov4.AddDays(1));

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetSites_ReturnsBadRequest_WhenCheckOutIsNotAfterCheckIn()
    {
        using var context = CreateContext();

        var result = await new SitesController(context).GetSites(10, Nov4, Nov4);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetSites_ReturnsBadRequest_WhenTheCampgroundIsClosedForTheSeason()
    {
        using var context = CreateContext();

        var result = await new SitesController(context).GetSites(20, Nov4, Nov4.AddDays(1));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Loft Mountain is closed for the season on Nov 4. It's open May 8 to Oct 31.", badRequest.Value);
    }

    [Fact]
    public async Task GetSites_IgnoresTheSignedInUsersOwnReservation_WhenAskedTo()
    {
        using var context = CreateContext();

        var result = await new SitesController(context).As(1).GetSites(10, Nov4.AddDays(1), Nov4.AddDays(2), except: "NPS-AAAAAA");

        Assert.All(result.Value!, s => Assert.True(s.Available));
    }

    [Fact]
    public async Task GetSites_StillCountsAnotherUsersReservation_WhenAskedToIgnoreIt()
    {
        using var context = CreateContext();

        var result = await new SitesController(context).As(2).GetSites(10, Nov4.AddDays(1), Nov4.AddDays(2), except: "NPS-AAAAAA");

        Assert.False(result.Value!.Single(s => s.SiteNumber == "01").Available);
    }

    [Fact]
    public async Task GetSites_LeavesOutRetiredSites_AndSortsByLoop()
    {
        using var context = CreateContext();
        context.Sites.AddRange(
            new Site { SiteId = 103, CampgroundId = 10, SiteNumber = "03", SiteType = "Tent", MaxGuests = 6, Retired = true },
            new Site { SiteId = 104, CampgroundId = 10, SiteNumber = "A001", SiteType = "Tent", MaxGuests = 6, Loop = "Loop A" });
        context.SaveChanges();

        var result = await new SitesController(context).GetSites(10, Nov4.AddDays(4), Nov4.AddDays(5));

        Assert.Equal(new[] { "01", "02", "A001" }, result.Value!.Select(s => s.SiteNumber));
        Assert.Equal("Loop A", result.Value!.Last().Loop);
    }
}
