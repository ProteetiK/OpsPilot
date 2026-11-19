
using Microsoft.EntityFrameworkCore;
using OpsPilot.Application.Incidents;
using OpsPilot.Domain.Entities;
using OpsPilot.Domain.Enums;
using OpsPilot.Infrastructure.Persistence;

namespace OpsPilot.Infrastructure.Incidents;

public sealed class IncidentService : IIncidentService
{
    private readonly AppDbContext _dbContext;

    public IncidentService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<IncidentSummary>> GetIncidentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Incidents
            .AsNoTracking()
            .OrderByDescending(incident => incident.CreatedAtUtc)
            .Select(incident => ToSummary(incident))
            .ToListAsync(cancellationToken);
    }

    public async Task<IncidentSummary?> GetIncidentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.Incidents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                incident => incident.Id == id,
                cancellationToken);

        return incident is null ? null : ToSummary(incident);
    }

    public async Task<IncidentSummary> CreateIncidentAsync(
        CreateIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<IncidentSeverity>(
                request.Severity, true, out var severity) ||
            !Enum.IsDefined(severity))
        {
            throw new ArgumentException("Invalid incident severity.");
        }

        if (request.ServiceId.HasValue)
        {
            var serviceExists = await _dbContext.Services
                .AnyAsync(
                    service => service.Id == request.ServiceId.Value,
                    cancellationToken);

            if (!serviceExists)
            {
                throw new ArgumentException("The specified service does not exist.");
            }
        }

        var incident = new Incident
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Severity = severity,
            Status = IncidentStatus.Open,
            ServiceId = request.ServiceId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        if (string.IsNullOrWhiteSpace(incident.Title))
            throw new ArgumentException("Incident title is required.");

        if (string.IsNullOrWhiteSpace(incident.Description))
            throw new ArgumentException("Incident description is required.");

        if (incident.Title.Length > 200)
            throw new ArgumentException("Incident title cannot exceed 200 characters.");

        if (incident.Description.Length > 5000)
            throw new ArgumentException("Incident description cannot exceed 5000 characters.");

        _dbContext.Incidents.Add(incident);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToSummary(incident);
    }

    public async Task<bool> UpdateStatusAsync(
        Guid id,
        UpdateIncidentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<IncidentStatus>(
                request.Status, true, out var status) ||
            !Enum.IsDefined(status))
        {
            throw new ArgumentException("Invalid incident status.");
        }

        var incident = await _dbContext.Incidents
            .FirstOrDefaultAsync(
                incident => incident.Id == id,
                cancellationToken);

        if (incident is null)
            return false;

        incident.Status = status;
        incident.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> AssignIncidentAsync(
        Guid id,
        Guid? assignedToUserId,
        CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.Incidents
            .FirstOrDefaultAsync(
                incident => incident.Id == id,
                cancellationToken);

        if (incident is null)
            return false;

        if (assignedToUserId.HasValue)
        {
            var userExists = await _dbContext.Users
                .AnyAsync(
                    user => user.Id == assignedToUserId.Value && user.IsActive,
                    cancellationToken);

            if (!userExists)
                throw new ArgumentException("The assigned user does not exist or is inactive.");
        }

        incident.AssignedToUserId = assignedToUserId;
        incident.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static IncidentSummary ToSummary(Incident incident)
    {
        return new IncidentSummary(
            incident.Id,
            incident.Title,
            incident.Description,
            incident.Severity.ToString(),
            incident.Status.ToString(),
            incident.ServiceId,
            incident.AssignedToUserId,
            incident.CreatedAtUtc,
            incident.UpdatedAtUtc);
    }
}
