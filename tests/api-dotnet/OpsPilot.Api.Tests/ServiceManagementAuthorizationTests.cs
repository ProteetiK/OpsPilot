
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpsPilot.Api.Contracts;
using OpsPilot.Application.Services;
using OpsPilot.Domain.Entities;
using OpsPilot.Domain.Enums;
using OpsPilot.Infrastructure.Persistence;

namespace OpsPilot.Api.Tests;

public class ServiceManagementAuthorizationTests
{
    private const string Password = "Test-Password-123!";

    [Theory]
    [InlineData(UserRole.Viewer)]
    [InlineData(UserRole.Engineer)]
    [InlineData(UserRole.Administrator)]
    public async Task AllRoles_CanViewServices_Returns200(UserRole role)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "user@opspilot.local", Password, role: role);

        await AuthenticateAsync(client, "user@opspilot.local");

        var response = await client.GetAsync("/api/services");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(UserRole.Viewer)]
    [InlineData(UserRole.Engineer)]
    public async Task NonAdministrators_CannotCreateServices_Returns403(
        UserRole role)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "user@opspilot.local", Password, role: role);

        await AuthenticateAsync(client, "user@opspilot.local");

        var response = await client.PostAsJsonAsync(
            "/api/services",
            new CreateServiceRequest(
                "Payment Service",
                "Processes payment transactions."));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Administrator_CanCreateService_Returns201()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "admin@opspilot.local",
            Password,
            role: UserRole.Administrator);

        await AuthenticateAsync(client, "admin@opspilot.local");

        var response = await client.PostAsJsonAsync(
            "/api/services",
            new CreateServiceRequest(
                "Payment Service",
                "Processes payment transactions."));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var service = await response.Content
            .ReadFromJsonAsync<ServiceSummary>();

        Assert.NotNull(service);
        Assert.Equal("Payment Service", service.Name);
    }

    [Fact]
    public async Task Administrator_CanUpdateService_Returns204()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "admin@opspilot.local",
            Password,
            role: UserRole.Administrator);

        var serviceId = await SeedServiceAsync(factory);

        await AuthenticateAsync(client, "admin@opspilot.local");

        var response = await client.PutAsJsonAsync(
            $"/api/services/{serviceId}",
            new UpdateServiceRequest(
                "Payment Processing",
                "Handles payment authorization and settlement."));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var updatedService = await db.Services
            .AsNoTracking()
            .SingleAsync(service => service.Id == serviceId);

        Assert.Equal("Payment Processing", updatedService.Name);
    }

    [Fact]
    public async Task Engineer_CannotUpdateService_Returns403()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "engineer@opspilot.local",
            Password,
            role: UserRole.Engineer);

        var serviceId = await SeedServiceAsync(factory);

        await AuthenticateAsync(client, "engineer@opspilot.local");

        var response = await client.PutAsJsonAsync(
            $"/api/services/{serviceId}",
            new UpdateServiceRequest(
                "Modified Service",
                "Modified description."));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

    private static async Task<Guid> SeedServiceAsync(
        CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        var service = new Service
        {
            Name = "Checkout Service",
            Description = "Handles customer checkout."
        };

        db.Services.Add(service);
        await db.SaveChangesAsync();

        return service.Id;
    }
}
