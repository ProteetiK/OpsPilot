
using Microsoft.EntityFrameworkCore;
using OpsPilot.Application.Services;
using OpsPilot.Domain.Entities;
using OpsPilot.Infrastructure.Persistence;
using OpsPilot.Application.Common.Pagination;

namespace OpsPilot.Infrastructure.Services;

public sealed class ServiceManagement : IServiceManagement
{
    private readonly AppDbContext _dbContext;

    public ServiceManagement(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ServiceSummary>> GetServicesAsync(
    PaginationParameters parameters,
    string? search,
    CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Services
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();

            query = query.Where(service =>
                service.Name.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(service => service.Name)
            .ThenBy(service => service.Id)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(service => new ServiceSummary(
                service.Id,
                service.Name,
                service.Description,
                service.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<ServiceSummary>(
            items,
            parameters.Page,
            parameters.PageSize,
            totalCount);
    }

    public async Task<ServiceSummary?> GetServiceByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var service = await _dbContext.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(
                service => service.Id == id,
                cancellationToken);

        return service is null ? null : ToSummary(service);
    }

    public async Task<ServiceSummary> CreateServiceAsync(
        CreateServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.Description);

        var normalizedName = request.Name.Trim();

        var alreadyExists = await _dbContext.Services
            .AnyAsync(
                service => service.Name.ToUpper() == normalizedName.ToUpper(),
                cancellationToken);

        if (alreadyExists)
            throw new ArgumentException("A service with this name already exists.");

        var service = new Service
        {
            Name = normalizedName,
            Description = request.Description.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Services.Add(service);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToSummary(service);
    }

    public async Task<bool> UpdateServiceAsync(
        Guid id,
        UpdateServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.Description);

        var service = await _dbContext.Services
            .FirstOrDefaultAsync(
                service => service.Id == id,
                cancellationToken);

        if (service is null)
            return false;

        var normalizedName = request.Name.Trim();

        var duplicateExists = await _dbContext.Services
            .AnyAsync(
                existing => existing.Id != id &&
                            existing.Name.ToUpper() == normalizedName.ToUpper(),
                cancellationToken);

        if (duplicateExists)
            throw new ArgumentException("A service with this name already exists.");

        service.Name = normalizedName;
        service.Description = request.Description.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static void Validate(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Service name is required.");

        if (name.Trim().Length > 200)
            throw new ArgumentException("Service name cannot exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Service description is required.");

        if (description.Trim().Length > 2000)
            throw new ArgumentException("Service description cannot exceed 2000 characters.");
    }

    private static ServiceSummary ToSummary(Service service)
    {
        return new ServiceSummary(
            service.Id,
            service.Name,
            service.Description,
            service.CreatedAtUtc);
    }
}
