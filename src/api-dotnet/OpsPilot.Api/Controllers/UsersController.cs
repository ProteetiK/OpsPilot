
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsPilot.Application.Users;
using OpsPilot.Domain.Enums;

namespace OpsPilot.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "ManageUsers")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(
        IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserSummary>>> GetUsers(
        CancellationToken cancellationToken)
    {
        var users = await _userManagementService.GetUsersAsync(
            cancellationToken);

        return Ok(users);
    }

    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = "AssignRoles")]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var actorIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(actorIdValue, out var actorUserId))
        {
            return Unauthorized();
        }

        if (!Enum.IsDefined(typeof(UserRole), request.Role))
        {
            return BadRequest(new
            {
                message = "Invalid user role."
            });
        }

        var result = await _userManagementService.ChangeRoleAsync(
            actorUserId,
            id,
            request.Role,
            cancellationToken);

        return result switch
        {
            UserManagementResult.Success => NoContent(),

            UserManagementResult.UserNotFound => NotFound(),

            UserManagementResult.CannotChangeOwnRole => BadRequest(new
            {
                message = "You cannot change your own role."
            }),

            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}

public sealed record ChangeUserRoleRequest(UserRole Role);
