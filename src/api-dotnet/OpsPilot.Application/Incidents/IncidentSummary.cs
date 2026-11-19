namespace OpsPilot.Application.Incidents;

public sealed record IncidentSummary(
    Guid Id,
    string Title,
    string Description,
    string Severity,
    string Status,
    Guid? ServiceId,
    Guid? AssignedToUserId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);