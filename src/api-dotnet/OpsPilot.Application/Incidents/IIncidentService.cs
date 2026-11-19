
namespace OpsPilot.Application.Incidents;
using OpsPilot.Application.Common.Pagination;

public interface IIncidentService
{
    Task<PagedResult<IncidentSummary>> GetIncidentsAsync(
    PaginationParameters parameters,
    string? status,
    string? severity,
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
