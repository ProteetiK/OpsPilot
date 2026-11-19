
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
using OpsPilot.Application.Common.Pagination;

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

        var response = await client.GetAsync("/api/v1/services");

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
            "/api/v1/services",
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
            "/api/v1/services",
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
            $"/api/v1/services/{serviceId}",
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
            $"/api/v1/services/{serviceId}",
            new UpdateServiceRequest(
                "Modified Service",
                "Modified description."));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }


    [Fact]
    public async Task GetServices_ReturnsDefaultPagination()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local", Password, role: UserRole.Viewer);

        await SeedServicesAsync(factory, 25);

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var response = await client.GetAsync("/api/v1/services");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<ServiceSummary>>();

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(20, result.Items.Count);
    }

    [Fact]
    public async Task GetServices_ReturnsRequestedPage()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local", Password, role: UserRole.Viewer);

        await SeedServicesAsync(factory, 25);

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var response = await client.GetAsync(
            "/api/v1/services?page=2&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<ServiceSummary>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
    }

    [Fact]
    public async Task GetServices_SearchesByName()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local", Password, role: UserRole.Viewer);

        await SeedServicesAsync(factory, 0);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Services.AddRange(
                new Service
                {
                    Name = "Payment Service",
                    Description = "Processes payments."
                },
                new Service
                {
                    Name = "Checkout Service",
                    Description = "Handles checkout."
                },
                new Service
                {
                    Name = "Inventory Service",
                    Description = "Manages inventory."
                });

            await db.SaveChangesAsync();
        }

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var response = await client.GetAsync(
            "/api/v1/services?search=payment");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<ServiceSummary>>();

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Payment Service", result.Items[0].Name);
    }

    [Fact]
    public async Task GetServices_CombinesSearchAndPagination()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local", Password, role: UserRole.Viewer);

        await SeedServicesAsync(factory, 25);

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var response = await client.GetAsync(
            "/api/v1/services?search=Service&page=2&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<ServiceSummary>>();

        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.Items.Count);
    }

    [Fact]
    public async Task GetServices_ReturnsServicesInAlphabeticalOrder()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local", Password, role: UserRole.Viewer);

        await SeedServicesAsync(factory, 0);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Services.AddRange(
                new Service { Name = "Payment", Description = "Payments." },
                new Service { Name = "Checkout", Description = "Checkout." },
                new Service { Name = "Inventory", Description = "Inventory." });

            await db.SaveChangesAsync();
        }

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var response = await client.GetAsync("/api/v1/services");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<ServiceSummary>>();

        Assert.NotNull(result);

        Assert.Equal(
            new[] { "Checkout", "Inventory", "Payment" },
            result.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task GetServices_InvalidPagination_Returns400()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local", Password, role: UserRole.Viewer);

        await AuthenticateAsync(client, "viewer@opspilot.local");

        var response = await client.GetAsync(
            "/api/v1/services?page=0&pageSize=101");

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


    private static async Task SeedServicesAsync(
        CustomWebApplicationFactory factory,
        int count)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        var services = Enumerable.Range(1, count)
            .Select(index => new Service
            {
                Name = $"Service {index:D2}",
                Description = $"Test service {index}"
            })
            .ToList();

        db.Services.AddRange(services);
        await db.SaveChangesAsync();
    }

}
