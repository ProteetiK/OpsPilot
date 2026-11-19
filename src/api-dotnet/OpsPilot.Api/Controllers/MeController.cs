using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpsPilot.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public sealed class MeController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue("email");

        var role = User.FindFirstValue(ClaimTypes.Role);

        if (userId is null || email is null || role is null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            id = userId,
            email,
            role
        });
    }
}
