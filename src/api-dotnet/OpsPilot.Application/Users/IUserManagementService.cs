using OpsPilot.Domain.Enums;
using OpsPilot.Application.Common.Pagination;

namespace OpsPilot.Application.Users;

public interface IUserManagementService
{
    Task<PagedResult<UserSummary>> GetUsersAsync(
        PaginationParameters parameters,
        string? search,
        string? role,
        CancellationToken cancellationToken);

    Task<UserManagementResult> ChangeRoleAsync(
        Guid actorUserId,
        Guid targetUserId,
        UserRole newRole,
        CancellationToken cancellationToken);
}