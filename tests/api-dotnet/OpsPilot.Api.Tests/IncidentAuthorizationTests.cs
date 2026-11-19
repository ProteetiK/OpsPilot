
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

        var response = await client.GetAsync("/api/incidents");

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
            "/api/incidents",
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
            "/api/incidents",
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
            $"/api/incidents/{incidentId}/status",
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
            $"/api/incidents/{incidentId}/status",
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
            "/api/incidents",
            new CreateIncidentRequest(
                "Checkout failure",
                "Checkout requests are timing out.",
                "Urgent",
                null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task AuthenticateAsync(
        HttpClient client,
        string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
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
}
