namespace OpsPilot.Application.Incidents;

public sealed record CreateIncidentRequest(
    string Title,
    string Description,
    string Severity,
    Guid? ServiceId
);