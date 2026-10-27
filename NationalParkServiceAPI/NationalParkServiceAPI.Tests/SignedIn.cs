using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NationalParkServiceAPI.Tests;

public static class SignedIn
{
    // Makes the controller act as though the user with this id sent the request with a valid token.
    public static T As<T>(this T controller, int userId) where T : ControllerBase
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
        return controller;
    }
}
