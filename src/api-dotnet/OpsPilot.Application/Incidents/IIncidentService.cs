
namespace OpsPilot.Application.Incidents;

public interface IIncidentService
{
    Task<IReadOnlyList<IncidentSummary>> GetIncidentsAsync(
        CancellationToken cancellationToken = default);

    Task<IncidentSummary?> GetIncidentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IncidentSummary> CreateIncidentAsync(
        CreateIncidentRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusAsync(
        Guid id,
        UpdateIncidentStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> AssignIncidentAsync(
        Guid id,
        Guid? assignedToUserId,
        CancellationToken cancellationToken = default);
}
