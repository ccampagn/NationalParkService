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
            SitesLeft = 5,
            Badge = "Almost full",
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
        Assert.Equal(5, park.SitesLeft);
        Assert.Equal("Almost full", park.Badge);
        Assert.Equal(new[] { "Wildlife", "Hiking" }, park.Activities);
        Assert.Equal("https://upload.wikimedia.org/katmai.jpg", park.ImageUrl);
        Assert.Equal("Photographer, CC BY-SA 2.0", park.ImageCredit);
        Assert.Equal("https://commons.wikimedia.org/wiki/File:Katmai.jpg", park.ImageSourceUrl);
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
}
