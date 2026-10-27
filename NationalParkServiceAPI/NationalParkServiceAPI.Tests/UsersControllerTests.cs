using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalParkServiceAPI.Controllers;
using NationalParkServiceAPI.Data;
using NationalParkServiceAPI.Models;
using Xunit;

namespace NationalParkServiceAPI.Tests;

public class UsersControllerTests
{
    private static NationalParkServiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NationalParkServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NationalParkServiceDbContext(options);
    }

    private static SignUpRequest ValidRequest(string email = "ada@example.com") => new(
        "Ada", "Lovelace", email, "correct-horse", "12 Trail Rd", "Apt 3", "Boulder", "co", "80302");

    [Fact]
    public async Task SignUp_SavesUserWithHashedPassword()
    {
        using var context = CreateContext();
        var controller = new UsersController(context);

        var result = await controller.SignUp(ValidRequest(" Ada@Example.com "));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var user = Assert.IsType<UserResponse>(created.Value);
        Assert.Equal("Ada", user.FirstName);
        Assert.Equal("ada@example.com", user.Email);
        Assert.Equal("CO", user.State);
        Assert.Equal("Apt 3", user.AddressLine2);

        var saved = Assert.Single(context.Users);
        Assert.NotEqual("correct-horse", saved.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(saved, saved.PasswordHash, "correct-horse"));
    }

    [Fact]
    public async Task SignUp_BlankAddressLine2IsStoredAsNull()
    {
        using var context = CreateContext();
        var controller = new UsersController(context);

        await controller.SignUp(ValidRequest() with { AddressLine2 = "  " });

        Assert.Null(Assert.Single(context.Users).AddressLine2);
    }

    [Theory]
    [InlineData("", "Lovelace", "ada@example.com", "correct-horse", "12 Trail Rd", "Boulder", "CO", "80302")]
    [InlineData("Ada", "Lovelace", "not-an-email", "correct-horse", "12 Trail Rd", "Boulder", "CO", "80302")]
    [InlineData("Ada", "Lovelace", "ada@example.com", "short", "12 Trail Rd", "Boulder", "CO", "80302")]
    [InlineData("Ada", "Lovelace", "ada@example.com", "correct-horse", "", "Boulder", "CO", "80302")]
    [InlineData("Ada", "Lovelace", "ada@example.com", "correct-horse", "12 Trail Rd", "Boulder", "Colorado", "80302")]
    [InlineData("Ada", "Lovelace", "ada@example.com", "correct-horse", "12 Trail Rd", "Boulder", "CO", "8030")]
    public async Task SignUp_RejectsInvalidRequests(
        string firstName, string lastName, string email, string password,
        string addressLine1, string city, string state, string postalCode)
    {
        using var context = CreateContext();
        var controller = new UsersController(context);

        var result = await controller.SignUp(new SignUpRequest(
            firstName, lastName, email, password, addressLine1, null, city, state, postalCode));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(context.Users);
    }

    [Fact]
    public async Task SignUp_RejectsDuplicateEmail()
    {
        using var context = CreateContext();
        var controller = new UsersController(context);
        await controller.SignUp(ValidRequest());

        var result = await controller.SignUp(ValidRequest("ADA@example.com"));

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Single(context.Users);
    }

    [Fact]
    public async Task LogIn_ReturnsBearerSignInForCorrectPassword()
    {
        using var context = CreateContext();
        var controller = new UsersController(context);
        await controller.SignUp(ValidRequest());

        var result = await controller.LogIn(new LogInRequest(" ADA@example.com", "correct-horse"));

        var signIn = Assert.IsType<Microsoft.AspNetCore.Mvc.SignInResult>(result);
        Assert.Equal(BearerTokenDefaults.AuthenticationScheme, signIn.AuthenticationScheme);
        Assert.Equal(
            Assert.Single(context.Users).UserId.ToString(),
            signIn.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [Theory]
    [InlineData("ada@example.com", "wrong-password")]
    [InlineData("nobody@example.com", "correct-horse")]
    public async Task LogIn_RejectsWrongEmailOrPassword(string email, string password)
    {
        using var context = CreateContext();
        var controller = new UsersController(context);
        await controller.SignUp(ValidRequest());

        var result = await controller.LogIn(new LogInRequest(email, password));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Email or password is incorrect.", unauthorized.Value);
    }

    [Fact]
    public async Task GetMe_ReturnsTheSignedInUser()
    {
        using var context = CreateContext();
        await new UsersController(context).SignUp(ValidRequest());
        var userId = Assert.Single(context.Users).UserId;

        var result = await new UsersController(context).As(userId).GetMe();

        Assert.Equal("ada@example.com", result.Value!.Email);
    }

    [Fact]
    public async Task GetMe_ReturnsUnauthorizedWhenTheUserNoLongerExists()
    {
        using var context = CreateContext();

        var result = await new UsersController(context).As(42).GetMe();

        Assert.IsType<UnauthorizedResult>(result.Result);
    }
}
