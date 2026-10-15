using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NationalParkServiceAPI.Controllers;
using NationalParkServiceAPI.Data;
using NationalParkServiceAPI.Models;
using Xunit;

namespace NationalParkServiceAPI.Tests;

// Runs against the real NationalParkService database, using the API's connection string.
public class ParksControllerTests
{
    private static NationalParkServiceDbContext CreateContext()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        var options = new DbContextOptionsBuilder<NationalParkServiceDbContext>()
            .UseSqlServer(configuration.GetConnectionString("NationalParkService"))
            .Options;
        return new NationalParkServiceDbContext(options);
    }

    [Fact]
    public async Task GetParks_ReturnsAllParksFromDatabaseSortedByName()
    {
        using var context = CreateContext();
        var expected = await context.Parks.AsNoTracking().Select(p => p.ParkName).ToListAsync();
        expected.Sort(StringComparer.OrdinalIgnoreCase);

        var controller = new ParksController(context);

        var result = await controller.GetParks();

        var parks = Assert.IsAssignableFrom<IEnumerable<Park>>(result.Value);
        Assert.NotEmpty(parks);
        Assert.Equal(expected, parks.Select(p => p.ParkName));
    }

    [Fact]
    public async Task GetParks_ReturnsDetailsForEveryPark()
    {
        using var context = CreateContext();
        var controller = new ParksController(context);

        var result = await controller.GetParks();

        var parks = Assert.IsAssignableFrom<IEnumerable<Park>>(result.Value);
        Assert.All(parks, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.State), $"{p.ParkName} has no state");
            Assert.False(string.IsNullOrWhiteSpace(p.Region), $"{p.ParkName} has no region");
            Assert.False(string.IsNullOrWhiteSpace(p.Tagline), $"{p.ParkName} has no tagline");
            Assert.Contains(p.Scene, new[] { "peaks", "mesa", "coast", "forest" });
            Assert.InRange(p.Rating, 0m, 5m);
            Assert.True(p.BaseRate > 0, $"{p.ParkName} has no base rate");
            Assert.NotEmpty(p.Activities);
            Assert.StartsWith("https://", p.ImageUrl);
            Assert.False(string.IsNullOrWhiteSpace(p.ImageCredit), $"{p.ParkName} photo has no credit");
            Assert.StartsWith("https://commons.wikimedia.org/", p.ImageSourceUrl);
        });
    }

    [Fact]
    public async Task GetParks_ReturnsEmptyList_WhenNoneExist()
    {
        using var context = CreateContext();
        // Delete inside a transaction that is never committed, so the real data is left untouched.
        using var transaction = await context.Database.BeginTransactionAsync();
        await context.Parks.ExecuteDeleteAsync();

        var controller = new ParksController(context);

        var result = await controller.GetParks();

        var parks = Assert.IsAssignableFrom<IEnumerable<Park>>(result.Value);
        Assert.Empty(parks);
    }
}
