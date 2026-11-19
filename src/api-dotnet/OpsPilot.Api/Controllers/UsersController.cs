using Asp.Versioning;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsPilot.Application.Users;
using OpsPilot.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using OpsPilot.Application.Common.Pagination;

namespace OpsPilot.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/users")]
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
    public async Task<ActionResult<PagedResult<UserSummary>>> GetUsers(
    [FromQuery] PaginationParameters pagination,
    [FromQuery] string? search,
    [FromQuery] string? role,
    CancellationToken cancellationToken)
    {
        try
        {
            var users = await _userManagementService.GetUsersAsync(
                pagination,
                search,
                role,
                cancellationToken);

            return Ok(users);
        }
        catch (ArgumentException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: exception.Message);
        }
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
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: "Invalid user role.");
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

            UserManagementResult.CannotChangeOwnRole => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: "You cannot change your own role."),

            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}

public sealed record ChangeUserRoleRequest(
    [Required]
    [EnumDataType(typeof(UserRole))]
    UserRole Role
);
