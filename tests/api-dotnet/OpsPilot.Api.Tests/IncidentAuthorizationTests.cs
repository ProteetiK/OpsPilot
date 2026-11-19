
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpsPilot.Api.Contracts;
using OpsPilot.Application.Incidents;
using OpsPilot.Domain.Entities;
using OpsPilot.Domain.Enums;
using OpsPilot.Infrastructure.Persistence;
using OpsPilot.Application.Common.Pagination;

namespace OpsPilot.Api.Tests;

public class IncidentAuthorizationTests
{
    private const string Password = "Test-Password-123!";

    [Theory]
    [InlineData(UserRole.Viewer)]
    [InlineData(UserRole.Engineer)]
    [InlineData(UserRole.Administrator)]
    public async Task AllRoles_CanViewIncidents_Returns200(UserRole role)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "user@opspilot.local", Password, role: role);

        await AuthenticateAsync(client, "user@opspilot.local");

        var response = await client.GetAsync("/api/v1/incidents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_CannotCreateIncident_Returns403()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local",
            Password,
            role: UserRole.Viewer);

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var response = await client.PostAsJsonAsync(
            "/api/v1/incidents",
            new CreateIncidentRequest(
                "Checkout failure",
                "Checkout requests are timing out.",
                "High",
                null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(UserRole.Engineer)]
    [InlineData(UserRole.Administrator)]
    public async Task AuthorizedRoles_CanCreateIncident_Returns201(
        UserRole role)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "user@opspilot.local", Password, role: role);

        await AuthenticateAsync(client, "user@opspilot.local");

        var response = await client.PostAsJsonAsync(
            "/api/v1/incidents",
            new CreateIncidentRequest(
                "Checkout failure",
                "Checkout requests are timing out.",
                "High",
                null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var incident = await response.Content
            .ReadFromJsonAsync<IncidentSummary>();

        Assert.NotNull(incident);
        Assert.Equal("Checkout failure", incident.Title);
        Assert.Equal("High", incident.Severity);
        Assert.Equal("Open", incident.Status);
    }

    [Fact]
    public async Task Viewer_CannotUpdateIncidentStatus_Returns403()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local",
            Password,
            role: UserRole.Viewer);

        var incidentId = await SeedIncidentAsync(factory);

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/incidents/{incidentId}/status",
            new UpdateIncidentStatusRequest("Investigating"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(UserRole.Engineer)]
    [InlineData(UserRole.Administrator)]
    public async Task AuthorizedRoles_CanUpdateIncidentStatus_Returns204(
        UserRole role)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "user@opspilot.local", Password, role: role);

        var incidentId = await SeedIncidentAsync(factory);

        await AuthenticateAsync(client, "user@opspilot.local");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/incidents/{incidentId}/status",
            new UpdateIncidentStatusRequest("Investigating"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var incident = await db.Incidents
            .AsNoTracking()
            .SingleAsync(item => item.Id == incidentId);

        Assert.Equal(IncidentStatus.Investigating, incident.Status);
    }

    [Fact]
    public async Task Engineer_CannotCreateIncidentWithInvalidSeverity_Returns400()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "engineer@opspilot.local",
            Password,
            role: UserRole.Engineer);

        await AuthenticateAsync(client, "engineer@opspilot.local");

        var response = await client.PostAsJsonAsync(
            "/api/v1/incidents",
            new CreateIncidentRequest(
                "Checkout failure",
                "Checkout requests are timing out.",
                "Urgent",
                null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Fact]
    public async Task GetIncidents_ReturnsDefaultPagination()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local", Password, role: UserRole.Viewer);

        await SeedIncidentsAsync(factory, 25);

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var response = await client.GetAsync("/api/v1/incidents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<IncidentSummary>>();

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(20, result.Items.Count);
    }

    [Fact]
    public async Task GetIncidents_ReturnsRequestedPage()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local", Password, role: UserRole.Viewer);

        await SeedIncidentsAsync(factory, 25);

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var response = await client.GetAsync(
            "/api/v1/incidents?page=2&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<IncidentSummary>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
    }

    [Theory]
    [InlineData("Open", 2)]
    [InlineData("Resolved", 1)]
    public async Task GetIncidents_FiltersByStatus(
        string status,
        int expectedCount)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local", Password, role: UserRole.Viewer);

        await SeedFilteredIncidentsAsync(factory);

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var response = await client.GetAsync(
            $"/api/v1/incidents?status={status}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<IncidentSummary>>();

        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(status, item.Status));
    }

    [Fact]
    public async Task GetIncidents_FiltersBySeverity()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local", Password, role: UserRole.Viewer);

        await SeedFilteredIncidentsAsync(factory);

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var response = await client.GetAsync(
            "/api/v1/incidents?severity=Critical");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<IncidentSummary>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(
            result.Items,
            item => Assert.Equal("Critical", item.Severity));
    }

    [Fact]
    public async Task GetIncidents_CombinesStatusAndSeverityFilters()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local", Password, role: UserRole.Viewer);

        await SeedFilteredIncidentsAsync(factory);

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var response = await client.GetAsync(
            "/api/v1/incidents?status=Open&severity=High");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<IncidentSummary>>();

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, item =>
        {
            Assert.Equal("Open", item.Status);
            Assert.Equal("High", item.Severity);
        });
    }

    [Theory]
    [InlineData("InvalidStatus", null)]
    [InlineData(null, "Urgent")]
    public async Task GetIncidents_InvalidFilter_Returns400(
        string? status,
        string? severity)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local", Password, role: UserRole.Viewer);

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var query = new List<string>();

        if (status is not null)
            query.Add($"status={status}");

        if (severity is not null)
            query.Add($"severity={severity}");

        var response = await client.GetAsync(
            $"/api/v1/incidents?{string.Join("&", query)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid request", body);
    }

    [Fact]
    public async Task GetIncidents_InvalidPageSize_Returns400()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local", Password, role: UserRole.Viewer);

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var response = await client.GetAsync(
            "/api/v1/incidents?page=0&pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    private static async Task AuthenticateAsync(
        HttpClient client,
        string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = Password });

        response.EnsureSuccessStatusCode();

        var login = await response.Content
            .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(login);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer", login.AccessToken);
    }

    private static async Task<Guid> SeedIncidentAsync(
        CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        var incident = new Incident
        {
            Title = "Payment service failure",
            Description = "Payment requests are failing.",
            Severity = IncidentSeverity.High,
            Status = IncidentStatus.Open
        };

        db.Incidents.Add(incident);
        await db.SaveChangesAsync();

        return incident.Id;
    }


    private static async Task SeedIncidentsAsync(
        CustomWebApplicationFactory factory,
        int count)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        var incidents = Enumerable.Range(1, count)
            .Select(index => new Incident
            {
                Title = $"Incident {index}",
                Description = $"Test incident {index}",
                Severity = IncidentSeverity.High,
                Status = IncidentStatus.Open,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-index),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-index)
            })
            .ToList();

        db.Incidents.AddRange(incidents);
        await db.SaveChangesAsync();
    }

    private static async Task SeedFilteredIncidentsAsync(
        CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        db.Incidents.AddRange(
            new Incident
            {
                Title = "Open high incident",
                Description = "High severity open incident",
                Status = IncidentStatus.Open,
                Severity = IncidentSeverity.High
            },
            new Incident
            {
                Title = "Open critical incident",
                Description = "Critical severity open incident",
                Status = IncidentStatus.Open,
                Severity = IncidentSeverity.Critical
            },
            new Incident
            {
                Title = "Resolved critical incident",
                Description = "Resolved critical incident",
                Status = IncidentStatus.Resolved,
                Severity = IncidentSeverity.Critical
            });

        await db.SaveChangesAsync();
    }

}
