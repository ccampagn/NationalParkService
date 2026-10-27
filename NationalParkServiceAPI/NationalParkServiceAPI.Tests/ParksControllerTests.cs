using Microsoft.EntityFrameworkCore;
using NationalParkServiceAPI.Controllers;
using NationalParkServiceAPI.Data;
using NationalParkServiceAPI.Models;
using Xunit;

namespace NationalParkServiceAPI.Tests;

public class ParksControllerTests
{
    private static NationalParkServiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NationalParkServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NationalParkServiceDbContext(options);
    }

    [Fact]
    public async Task GetParks_ReturnsAllParksSortedByName()
    {
        using var context = CreateContext();
        context.Parks.AddRange(
            new Park { ParkId = 1, ParkName = "Zion" },
            new Park { ParkId = 2, ParkName = "Acadia" },
            new Park { ParkId = 3, ParkName = "Denali" });
        await context.SaveChangesAsync();

        var controller = new ParksController(context);

        var result = await controller.GetParks();

        var parks = Assert.IsAssignableFrom<IEnumerable<Park>>(result.Value);
        Assert.Equal(new[] { "Acadia", "Denali", "Zion" }, parks.Select(p => p.ParkName));
    }

    [Fact]
    public async Task GetParks_ReturnsParkDetails()
    {
        using var context = CreateContext();
        context.Parks.Add(new Park
        {
            ParkId = 1,
            ParkName = "Katmai",
            State = "Alaska",
            Region = "Alaska",
            Scene = "coast",
            Tagline = "Brown bears fishing for salmon at Brooks Falls.",
            Rating = 4.9m,
            Reviews = 876,
            BaseRate = 20,
            Badge = "New sites",
            Activities = ["Wildlife", "Hiking"],
            ImageUrl = "https://upload.wikimedia.org/katmai.jpg",
            ImageCredit = "Photographer, CC BY-SA 2.0",
            ImageSourceUrl = "https://commons.wikimedia.org/wiki/File:Katmai.jpg",
        });
        await context.SaveChangesAsync();

        var controller = new ParksController(context);

        var result = await controller.GetParks();

        var park = Assert.Single(Assert.IsAssignableFrom<IEnumerable<Park>>(result.Value));
        Assert.Equal("Alaska", park.State);
        Assert.Equal("Alaska", park.Region);
        Assert.Equal("coast", park.Scene);
        Assert.Equal(4.9m, park.Rating);
        Assert.Equal(876, park.Reviews);
        Assert.Equal(20, park.BaseRate);
        Assert.Equal("New sites", park.Badge);
        Assert.Equal(new[] { "Wildlife", "Hiking" }, park.Activities);
        Assert.Equal("https://upload.wikimedia.org/katmai.jpg", park.ImageUrl);
        Assert.Equal("Photographer, CC BY-SA 2.0", park.ImageCredit);
        Assert.Equal("https://commons.wikimedia.org/wiki/File:Katmai.jpg", park.ImageSourceUrl);
    }

    [Fact]
    public async Task GetParks_IncludesEachParksCampgroundsSortedByName()
    {
        using var context = CreateContext();
        context.Parks.AddRange(
            new Park { ParkId = 1, ParkName = "Shenandoah" },
            new Park { ParkId = 2, ParkName = "Gateway Arch" });
        context.Campgrounds.AddRange(
            new Campground { CampgroundId = 1, ParkId = 1, CampgroundName = "Mathews Arm", NightlyRate = 30, MaxGuests = 6 },
            new Campground { CampgroundId = 2, ParkId = 1, CampgroundName = "Big Meadows", NightlyRate = 30, MaxGuests = 6 },
            new Campground { CampgroundId = 3, ParkId = 1, CampgroundName = "Loft Mountain", NightlyRate = 30, MaxGuests = 6 });
        await context.SaveChangesAsync();

        var controller = new ParksController(context);

        var result = await controller.GetParks();

        var parks = Assert.IsAssignableFrom<IEnumerable<Park>>(result.Value).ToList();
        var shenandoah = parks.Single(p => p.ParkName == "Shenandoah");
        Assert.Equal(new[] { "Big Meadows", "Loft Mountain", "Mathews Arm" }, shenandoah.Campgrounds.Select(c => c.CampgroundName));
        Assert.Empty(parks.Single(p => p.ParkName == "Gateway Arch").Campgrounds);
    }

    [Fact]
    public async Task GetParks_ReturnsEmptyList_WhenNoneExist()
    {
        using var context = CreateContext();
        var controller = new ParksController(context);

        var result = await controller.GetParks();

        var parks = Assert.IsAssignableFrom<IEnumerable<Park>>(result.Value);
        Assert.Empty(parks);
    }

    private static readonly DateOnly Nov4 = new(2026, 11, 4);

    private static NationalParkServiceDbContext CreateAvailabilityContext()
    {
        var context = CreateContext();
        context.Parks.AddRange(
            new Park { ParkId = 1, ParkName = "Shenandoah" },
            new Park { ParkId = 2, ParkName = "Glacier" },
            new Park { ParkId = 3, ParkName = "Arches" },
            new Park { ParkId = 4, ParkName = "Gateway Arch" });
        context.Campgrounds.AddRange(
            // Shenandoah: Big Meadows has one site booked and one free.
            new Campground { CampgroundId = 10, ParkId = 1, CampgroundName = "Big Meadows" },
            // Glacier: closed for the season in November.
            new Campground { CampgroundId = 20, ParkId = 2, CampgroundName = "Many Glacier", SeasonOpens = "05-22", SeasonCloses = "09-20" },
            // Arches: its only site is booked.
            new Campground { CampgroundId = 30, ParkId = 3, CampgroundName = "Devils Garden" });
        context.Sites.AddRange(
            new Site { SiteId = 101, CampgroundId = 10, SiteNumber = "01" },
            new Site { SiteId = 102, CampgroundId = 10, SiteNumber = "02" },
            new Site { SiteId = 201, CampgroundId = 20, SiteNumber = "01" },
            new Site { SiteId = 301, CampgroundId = 30, SiteNumber = "01" });
        context.Reservations.AddRange(
            new Reservation { ConfirmationCode = "NPS-AAAAAA", CampgroundId = 10, SiteId = 101, CheckIn = Nov4, CheckOut = Nov4.AddDays(3) },
            new Reservation { ConfirmationCode = "NPS-BBBBBB", CampgroundId = 30, SiteId = 301, CheckIn = Nov4.AddDays(2), CheckOut = Nov4.AddDays(4) });
        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task GetAvailableParks_LeavesOutParksThatAreClosedFullyBookedOrHaveNoCampgrounds()
    {
        using var context = CreateAvailabilityContext();

        var result = await new ParksController(context).GetAvailableParks(Nov4, Nov4.AddDays(3));

        Assert.Equal(new[] { new ParkAvailability(1, 1) }, result.Value);
    }

    [Fact]
    public async Task GetAvailableParks_CountsSitesFreeForTheWholeStay()
    {
        using var context = CreateAvailabilityContext();

        var result = await new ParksController(context).GetAvailableParks(Nov4.AddDays(4), Nov4.AddDays(6));

        Assert.Equal(new[] { new ParkAvailability(1, 2), new ParkAvailability(3, 1) }, result.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    public async Task GetAvailableParks_ReturnsBadRequest_ForInvalidStays(int nights)
    {
        using var context = CreateAvailabilityContext();

        var result = await new ParksController(context).GetAvailableParks(Nov4, Nov4.AddDays(nights));

        Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result.Result);
    }
}
