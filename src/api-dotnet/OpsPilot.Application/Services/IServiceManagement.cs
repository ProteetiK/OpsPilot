namespace OpsPilot.Application.Services;

public interface IServiceManagement
{
    Task<IReadOnlyList<ServiceSummary>> GetServicesAsync(
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