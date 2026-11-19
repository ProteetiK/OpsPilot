namespace OpsPilot.Application.Users;

public sealed record UserSummary(
    Guid Id,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAtUtc
);