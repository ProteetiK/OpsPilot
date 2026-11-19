namespace OpsPilot.Application.Services;

public sealed record ServiceSummary(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAtUtc
);