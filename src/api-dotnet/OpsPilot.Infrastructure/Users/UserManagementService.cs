
using Microsoft.EntityFrameworkCore;
using OpsPilot.Application.Users;
using OpsPilot.Domain.Entities;
using OpsPilot.Domain.Enums;
using OpsPilot.Infrastructure.Persistence;
using OpsPilot.Application.Common.Pagination;

namespace OpsPilot.Infrastructure.Users;

public sealed class UserManagementService : IUserManagementService
{
    private readonly AppDbContext _dbContext;

    public UserManagementService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<UserSummary>> GetUsersAsync(
    PaginationParameters parameters,
    string? search,
    string? role,
    CancellationToken cancellationToken)
    {
        var query = _dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();

            query = query.Where(user =>
                user.Email.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!Enum.TryParse<UserRole>(role, true, out var parsedRole) ||
                !Enum.IsDefined(typeof(UserRole), parsedRole))
            {
                throw new ArgumentException("Invalid user role.");
            }

            query = query.Where(user => user.Role == parsedRole);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(user => user.Email)
            .ThenBy(user => user.Id)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(user => new UserSummary(
                user.Id,
                user.Email,
                user.Role.ToString(),
                user.IsActive,
                user.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<UserSummary>(
            items,
            parameters.Page,
            parameters.PageSize,
            totalCount);
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
