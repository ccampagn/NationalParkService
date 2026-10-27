namespace NationalParkServiceAPI.Models;

public record SignUpRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode);
