namespace NationalParkServiceAPI.Models;

// What a cancelled reservation refunds, after the fees kept under the cancellation policy.
public record CancellationResponse(string Code, int Refund);
