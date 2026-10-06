using Microsoft.EntityFrameworkCore;
using NationalParkServiceAPI.Controllers;
using NationalParkServiceAPI.Data;
using NationalParkServiceAPI.Models;
using Xunit;

namespace NationalParkServiceAPI.Tests;

public class PassTypesControllerTests
{
    private static NationalParkServiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NationalParkServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NationalParkServiceDbContext(options);
    }

    [Fact]
    public async Task GetPassTypes_ReturnsAllPassTypes()
    {
        using var context = CreateContext();
        context.PassTypes.AddRange(
            new PassType { PassTypeId = 1, PassTypeName = "Annual Pass", Cost = 80, ValidPeriod = 365, Description = "desc" },
            new PassType { PassTypeId = 2, PassTypeName = "Senior Pass", Cost = 20, ValidPeriod = 365, Description = "desc" });
        await context.SaveChangesAsync();

        var controller = new PassTypesController(context);

        var result = await controller.GetPassTypes();

        var passTypes = Assert.IsAssignableFrom<IEnumerable<PassType>>(result.Value);
        Assert.Equal(2, passTypes.Count());
    }

    [Fact]
    public async Task GetPassTypes_ReturnsEmptyList_WhenNoneExist()
    {
        using var context = CreateContext();
        var controller = new PassTypesController(context);

        var result = await controller.GetPassTypes();

        var passTypes = Assert.IsAssignableFrom<IEnumerable<PassType>>(result.Value);
        Assert.Empty(passTypes);
    }
}
