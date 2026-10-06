namespace NationalParkServiceAPI.Models;

public record PassRequest(int? PassTypeId, DateTime? IssueDate, DateTime? ExpirationDate, int? Active);
