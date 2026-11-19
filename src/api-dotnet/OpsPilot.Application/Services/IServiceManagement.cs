using OpsPilot.Application.Common.Pagination;

namespace OpsPilot.Application.Services;

public interface IServiceManagement
{
    Task<PagedResult<ServiceSummary>> GetServicesAsync(
        PaginationParameters parameters,
        string? search,
        CancellationToken cancellationToken = default);

    Task<ServiceSummary?> GetServiceByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ServiceSummary> CreateServiceAsync(
        CreateServiceRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateServiceAsync(
        Guid id,
        UpdateServiceRequest request,
        CancellationToken cancellationToken = default);
}