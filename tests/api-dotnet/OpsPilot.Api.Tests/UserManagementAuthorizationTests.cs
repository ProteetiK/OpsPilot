
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpsPilot.Domain.Enums;
using OpsPilot.Infrastructure.Persistence;
using OpsPilot.Api.Contracts;

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

        var response = await client.GetAsync("/api/users");

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

        var response = await client.GetAsync("/api/users");

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

        var response = await client.GetAsync("/api/users");

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
            $"/api/users/{targetId}/role",
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
            $"/api/users/{adminId}/role",
            new { role = UserRole.Viewer });

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
