using System.Security.Claims;

namespace NationalParkServiceAPI.Controllers;

public static class ClaimsPrincipalExtensions
{
    // The id of the signed-in user, from the claim added at log in. Only call this from [Authorize] actions.
    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
