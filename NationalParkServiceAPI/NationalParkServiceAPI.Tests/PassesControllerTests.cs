using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalParkServiceAPI.Controllers;
using NationalParkServiceAPI.Data;
using NationalParkServiceAPI.Models;
using Xunit;

namespace NationalParkServiceAPI.Tests;

public class PassesControllerTests
{
    private static NationalParkServiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NationalParkServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NationalParkServiceDbContext(options);
    }

    private static PassType SeedPassType(NationalParkServiceDbContext context, int id = 1)
    {
        var passType = new PassType { PassTypeId = id, PassTypeName = "Annual Pass", Cost = 80, ValidPeriod = 365, Description = "desc" };
        context.PassTypes.Add(passType);
        context.SaveChanges();
        return passType;
    }

    [Fact]
    public async Task GetPasses_ReturnsAllPasses_WithPassType()
    {
        using var context = CreateContext();
        var passType = SeedPassType(context);
        context.Passes.Add(new Pass { PassTypeId = passType.PassTypeId, IssueDate = DateTime.UtcNow, Active = 1 });
        await context.SaveChangesAsync();

        var controller = new PassesController(context);

        var result = await controller.GetPasses();

        var passes = Assert.IsAssignableFrom<IEnumerable<Pass>>(result.Value).ToList();
        var pass = Assert.Single(passes);
        Assert.NotNull(pass.PassType);
        Assert.Equal("Annual Pass", pass.PassType!.PassTypeName);
    }

    [Fact]
    public async Task GetPass_ReturnsPass_WhenItExists()
    {
        using var context = CreateContext();
        var passType = SeedPassType(context);
        var pass = new Pass { PassTypeId = passType.PassTypeId, IssueDate = DateTime.UtcNow, Active = 1 };
        context.Passes.Add(pass);
        await context.SaveChangesAsync();

        var controller = new PassesController(context);

        var result = await controller.GetPass(pass.PassId);

        Assert.Equal(pass.PassId, result.Value!.PassId);
    }

    [Fact]
    public async Task GetPass_ReturnsNotFound_WhenItDoesNotExist()
    {
        using var context = CreateContext();
        var controller = new PassesController(context);

        var result = await controller.GetPass(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreatePass_ReturnsCreated_WhenPassTypeExists()
    {
        using var context = CreateContext();
        var passType = SeedPassType(context);
        var controller = new PassesController(context);
        var request = new PassRequest(passType.PassTypeId, DateTime.UtcNow, null, 1);

        var result = await controller.CreatePass(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var pass = Assert.IsType<Pass>(created.Value);
        Assert.Equal(passType.PassTypeId, pass.PassTypeId);
        Assert.Equal(1, pass.Active);
        Assert.Single(context.Passes);
    }

    [Fact]
    public async Task CreatePass_ReturnsBadRequest_WhenPassTypeDoesNotExist()
    {
        using var context = CreateContext();
        var controller = new PassesController(context);
        var request = new PassRequest(999, DateTime.UtcNow, null, 1);

        var result = await controller.CreatePass(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(context.Passes);
    }

    [Fact]
    public async Task UpdatePass_UpdatesFields_WhenPassAndPassTypeExist()
    {
        using var context = CreateContext();
        var passType = SeedPassType(context, 1);
        var newPassType = SeedPassType(context, 2);
        var pass = new Pass { PassTypeId = passType.PassTypeId, IssueDate = DateTime.UtcNow, Active = 1 };
        context.Passes.Add(pass);
        await context.SaveChangesAsync();

        var controller = new PassesController(context);
        var newExpiration = DateTime.UtcNow.AddYears(1);
        var request = new PassRequest(newPassType.PassTypeId, pass.IssueDate, newExpiration, 0);

        var result = await controller.UpdatePass(pass.PassId, request);

        Assert.IsType<NoContentResult>(result);
        var updated = await context.Passes.FindAsync(pass.PassId);
        Assert.Equal(newPassType.PassTypeId, updated!.PassTypeId);
        Assert.Equal(newExpiration, updated.ExpirationDate);
        Assert.Equal(0, updated.Active);
    }

    [Fact]
    public async Task UpdatePass_ReturnsNotFound_WhenPassDoesNotExist()
    {
        using var context = CreateContext();
        var passType = SeedPassType(context);
        var controller = new PassesController(context);
        var request = new PassRequest(passType.PassTypeId, DateTime.UtcNow, null, 1);

        var result = await controller.UpdatePass(999, request);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdatePass_ReturnsBadRequest_WhenPassTypeDoesNotExist()
    {
        using var context = CreateContext();
        var passType = SeedPassType(context);
        var pass = new Pass { PassTypeId = passType.PassTypeId, IssueDate = DateTime.UtcNow, Active = 1 };
        context.Passes.Add(pass);
        await context.SaveChangesAsync();

        var controller = new PassesController(context);
        var request = new PassRequest(999, pass.IssueDate, null, 1);

        var result = await controller.UpdatePass(pass.PassId, request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeletePass_RemovesPass_WhenItExists()
    {
        using var context = CreateContext();
        var passType = SeedPassType(context);
        var pass = new Pass { PassTypeId = passType.PassTypeId, IssueDate = DateTime.UtcNow, Active = 1 };
        context.Passes.Add(pass);
        await context.SaveChangesAsync();

        var controller = new PassesController(context);

        var result = await controller.DeletePass(pass.PassId);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.Passes);
    }

    [Fact]
    public async Task DeletePass_ReturnsNotFound_WhenItDoesNotExist()
    {
        using var context = CreateContext();
        var controller = new PassesController(context);

        var result = await controller.DeletePass(999);

        Assert.IsType<NotFoundResult>(result);
    }
}
