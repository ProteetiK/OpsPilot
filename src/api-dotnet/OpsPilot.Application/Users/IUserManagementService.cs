using OpsPilot.Domain.Enums;

namespace OpsPilot.Application.Users;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserSummary>> GetUsersAsync(
        CancellationToken cancellationToken);

    Task<UserManagementResult> ChangeRoleAsync(
        Guid actorUserId,
        Guid targetUserId,
        UserRole newRole,
        CancellationToken cancellationToken);
}