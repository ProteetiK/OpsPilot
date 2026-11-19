
using Microsoft.EntityFrameworkCore;
using OpsPilot.Application.Users;
using OpsPilot.Domain.Entities;
using OpsPilot.Domain.Enums;
using OpsPilot.Infrastructure.Persistence;

namespace OpsPilot.Infrastructure.Users;

public sealed class UserManagementService : IUserManagementService
{
    private readonly AppDbContext _dbContext;

    public UserManagementService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new UserSummary(
                user.Id,
                user.Email,
                user.Role.ToString(),
                user.IsActive,
                user.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserManagementResult> ChangeRoleAsync(
        Guid actorUserId,
        Guid targetUserId,
        UserRole newRole,
        CancellationToken cancellationToken)
    {
        if (actorUserId == targetUserId)
        {
            return UserManagementResult.CannotChangeOwnRole;
        }

        var targetUser = await _dbContext.Users
            .FirstOrDefaultAsync(
                user => user.Id == targetUserId,
                cancellationToken);

        if (targetUser is null)
        {
            return UserManagementResult.UserNotFound;
        }

        var oldRole = targetUser.Role;

        if (oldRole == newRole)
        {
            return UserManagementResult.Success;
        }

        targetUser.Role = newRole;

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            Action = "UserRoleChanged",
            OldValue = oldRole.ToString(),
            NewValue = newRole.ToString(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.AuditLogs.Add(auditLog);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UserManagementResult.Success;
    }
}
