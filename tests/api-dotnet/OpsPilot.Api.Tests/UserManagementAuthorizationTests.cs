
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpsPilot.Domain.Enums;
using OpsPilot.Infrastructure.Persistence;
using OpsPilot.Api.Contracts;
using OpsPilot.Application.Common.Pagination;
using OpsPilot.Application.Users;

namespace OpsPilot.Api.Tests;

public class UserManagementAuthorizationTests
{
    private const string Password = "Test-Password-123!";

    [Fact]
    public async Task Viewer_CannotAccessUserManagement_Returns403()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "viewer@opspilot.local",
            Password,
            role: UserRole.Viewer);

        await AuthenticateAsync(
            client, "viewer@opspilot.local");

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Engineer_CannotManageUsers_Returns403()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "engineer@opspilot.local",
            Password,
            role: UserRole.Engineer);

        await AuthenticateAsync(
            client, "engineer@opspilot.local");

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Administrator_CanListUsers_Returns200()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "admin@opspilot.local",
            Password,
            role: UserRole.Administrator);

        await factory.SeedUserAsync(
            "viewer@opspilot.local",
            Password,
            role: UserRole.Viewer);

        await AuthenticateAsync(
            client, "admin@opspilot.local");

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Administrator_CanChangeAnotherUsersRole_AndAuditIsCreated()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "admin@opspilot.local",
            Password,
            role: UserRole.Administrator);

        await factory.SeedUserAsync(
            "viewer@opspilot.local",
            Password,
            role: UserRole.Viewer);

        var actorId = await GetUserIdAsync(
            factory, "admin@opspilot.local");

        var targetId = await GetUserIdAsync(
            factory, "viewer@opspilot.local");

        await AuthenticateAsync(
            client, "admin@opspilot.local");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/users/{targetId}/role",
            new { role = UserRole.Engineer });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var updatedUser = await db.Users
            .AsNoTracking()
            .SingleAsync(user => user.Id == targetId);

        Assert.Equal(UserRole.Engineer, updatedUser.Role);

        var audit = await db.AuditLogs
            .AsNoTracking()
            .SingleAsync(log => log.TargetUserId == targetId);

        Assert.Equal(actorId, audit.ActorUserId);
        Assert.Equal("UserRoleChanged", audit.Action);
        Assert.Equal("Viewer", audit.OldValue);
        Assert.Equal("Engineer", audit.NewValue);
    }

    [Fact]
    public async Task Administrator_CannotChangeOwnRole_Returns400()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "admin@opspilot.local",
            Password,
            role: UserRole.Administrator);

        var adminId = await GetUserIdAsync(
            factory, "admin@opspilot.local");

        await AuthenticateAsync(
            client, "admin@opspilot.local");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/users/{adminId}/role",
            new { role = UserRole.Viewer });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_ReturnsDefaultPagination()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "admin@opspilot.local", Password,
            role: UserRole.Administrator);

        for (var i = 1; i <= 24; i++)
        {
            await factory.SeedUserAsync(
                $"user{i:D2}@opspilot.local", Password);
        }

        await AuthenticateAsync(client, "admin@opspilot.local");

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<UserSummary>>();

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(20, result.Items.Count);
    }

    [Fact]
    public async Task GetUsers_ReturnsRequestedPage()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "admin@opspilot.local", Password,
            role: UserRole.Administrator);

        for (var i = 1; i <= 24; i++)
        {
            await factory.SeedUserAsync(
                $"user{i:D2}@opspilot.local", Password);
        }

        await AuthenticateAsync(client, "admin@opspilot.local");

        var response = await client.GetAsync(
            "/api/v1/users?page=2&pageSize=10");

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<UserSummary>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
    }

    [Fact]
    public async Task GetUsers_SearchesByEmail()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "admin@opspilot.local", Password,
            role: UserRole.Administrator);

        await factory.SeedUserAsync("engineer@example.com", Password);
        await factory.SeedUserAsync("viewer@example.com", Password);
        await factory.SeedUserAsync("other@example.com", Password);

        await AuthenticateAsync(client, "admin@opspilot.local");

        var response = await client.GetAsync(
            "/api/v1/users?search=example");

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<UserSummary>>();

        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, user =>
            Assert.Contains("example", user.Email));
    }

    [Fact]
    public async Task GetUsers_FiltersByRole()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "admin@opspilot.local", Password,
            role: UserRole.Administrator);

        await factory.SeedUserAsync(
            "engineer@example.com", Password,
            role: UserRole.Engineer);

        await factory.SeedUserAsync(
            "viewer@example.com", Password,
            role: UserRole.Viewer);

        await AuthenticateAsync(client, "admin@opspilot.local");

        var response = await client.GetAsync(
            "/api/v1/users?role=Engineer");

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<UserSummary>>();

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, user =>
            Assert.Equal("Engineer", user.Role));
    }

    [Fact]
    public async Task GetUsers_CombinesSearchAndRoleFilters()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "admin@opspilot.local", Password,
            role: UserRole.Administrator);

        await factory.SeedUserAsync(
            "engineer1@example.com", Password,
            role: UserRole.Engineer);

        await factory.SeedUserAsync(
            "engineer2@example.com", Password,
            role: UserRole.Engineer);

        await factory.SeedUserAsync(
            "viewer@example.com", Password,
            role: UserRole.Viewer);

        await AuthenticateAsync(client, "admin@opspilot.local");

        var response = await client.GetAsync(
            "/api/v1/users?search=engineer&role=Engineer");

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<UserSummary>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, user =>
        {
            Assert.Contains("engineer", user.Email);
            Assert.Equal("Engineer", user.Role);
        });
    }

    [Fact]
    public async Task GetUsers_InvalidRole_Returns400()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "admin@opspilot.local", Password,
            role: UserRole.Administrator);

        await AuthenticateAsync(client, "admin@opspilot.local");

        var response = await client.GetAsync(
            "/api/v1/users?role=SuperAdmin");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Administrator_InvalidRole_ReturnsProblemDetails()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            "admin@opspilot.local",
            Password,
            role: UserRole.Administrator);

        var targetId = Guid.NewGuid();

        await AuthenticateAsync(client, "admin@opspilot.local");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/users/{targetId}/role",
            new { role = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Administrator_CannotChangeOwnRole_ReturnsProblemDetails()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var adminId = Guid.NewGuid();

        await factory.SeedUserAsync(
            "admin@opspilot.local",
            Password,
            role: UserRole.Administrator);

        await AuthenticateAsync(client, "admin@opspilot.local");

        // Retrieve the authenticated user's ID from the database.
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var user = await db.Users.SingleAsync(
            u => u.Email == "admin@opspilot.local");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/users/{user.Id}/role",
            new { role = UserRole.Engineer });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
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

    private static async Task<Guid> GetUserIdAsync(
        CustomWebApplicationFactory factory,
        string email)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        return await db.Users
            .Where(user => user.Email == email)
            .Select(user => user.Id)
            .SingleAsync();
    }
}
