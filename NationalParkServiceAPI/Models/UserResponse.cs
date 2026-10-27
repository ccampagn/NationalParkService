namespace NationalParkServiceAPI.Models;

// A user's profile without the password hash.
public record UserResponse(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode);
