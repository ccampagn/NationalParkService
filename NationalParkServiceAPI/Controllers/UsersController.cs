using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalParkServiceAPI.Data;
using NationalParkServiceAPI.Models;

namespace NationalParkServiceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public partial class UsersController : ControllerBase
{
    public const int MinPasswordLength = 8;

    private readonly NationalParkServiceDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public UsersController(NationalParkServiceDbContext context, TimeProvider? timeProvider = null)
    {
        _context = context;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    // GET: api/users/me
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        var userId = User.GetUserId();
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            // The account was removed after the token was issued.
            return Unauthorized();
        }

        return ToResponse(user);
    }

    // POST: api/users/login
    // On success the response body is { tokenType, accessToken, expiresIn, refreshToken }.
    [HttpPost("login")]
    public async Task<IActionResult> LogIn(LogInRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Same message whether the email or the password is wrong, so the response doesn't reveal which emails have accounts.
        if (user == null ||
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password ?? string.Empty) == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Email or password is incorrect.");
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
            ],
            BearerTokenDefaults.AuthenticationScheme);

        return SignIn(new ClaimsPrincipal(identity), BearerTokenDefaults.AuthenticationScheme);
    }

    // POST: api/users
    [HttpPost]
    public async Task<ActionResult<UserResponse>> SignUp(SignUpRequest request)
    {
        var firstName = request.FirstName?.Trim() ?? string.Empty;
        var lastName = request.LastName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var addressLine1 = request.AddressLine1?.Trim() ?? string.Empty;
        var addressLine2 = string.IsNullOrWhiteSpace(request.AddressLine2) ? null : request.AddressLine2.Trim();
        var city = request.City?.Trim() ?? string.Empty;
        var state = request.State?.Trim().ToUpperInvariant() ?? string.Empty;
        var postalCode = request.PostalCode?.Trim() ?? string.Empty;

        if (firstName.Length == 0 || lastName.Length == 0)
        {
            return BadRequest("First and last name are required.");
        }
        if (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)
        {
            return BadRequest("Enter a valid email address.");
        }
        if ((request.Password ?? string.Empty).Length < MinPasswordLength)
        {
            return BadRequest($"Password must be at least {MinPasswordLength} characters.");
        }
        if (addressLine1.Length == 0 || city.Length == 0)
        {
            return BadRequest("Street address and city are required.");
        }
        if (!StatePattern().IsMatch(state))
        {
            return BadRequest("State must be a two-letter code, like CA.");
        }
        if (!PostalCodePattern().IsMatch(postalCode))
        {
            return BadRequest("ZIP code must be 5 digits, or ZIP+4 like 12345-6789.");
        }
        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict("An account with that email already exists.");
        }

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            AddressLine1 = addressLine1,
            AddressLine2 = addressLine2,
            City = city,
            State = state,
            PostalCode = postalCode,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password!);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMe), ToResponse(user));
    }

    private static UserResponse ToResponse(User user) => new(
        user.UserId,
        user.FirstName,
        user.LastName,
        user.Email,
        user.AddressLine1,
        user.AddressLine2,
        user.City,
        user.State,
        user.PostalCode);

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex StatePattern();

    [GeneratedRegex(@"^\d{5}(-\d{4})?$")]
    private static partial Regex PostalCodePattern();
}
