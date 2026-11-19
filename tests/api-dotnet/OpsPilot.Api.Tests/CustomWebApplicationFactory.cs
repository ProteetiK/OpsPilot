
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Identity;
using OpsPilot.Domain.Entities;
using OpsPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OpsPilot.Domain.Enums;

namespace OpsPilot.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestSecret =
        "OpsPilot-Integration-Test-Secret-1234567890";

    private readonly string _databaseName =
        $"OpsPilotTests_{Guid.NewGuid()}";

    // Set configuration before the minimal-hosting Program starts.
    static CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Host=localhost;Database=opspilot_tests;Username=test;Password=test");

        Environment.SetEnvironmentVariable(
            "Jwt__Secret", TestSecret);

        Environment.SetEnvironmentVariable(
            "Jwt__Issuer", "OpsPilot.Tests");

        Environment.SetEnvironmentVariable(
            "Jwt__Audience", "OpsPilot.Tests.Client");

        Environment.SetEnvironmentVariable(
            "Jwt__AccessTokenMinutes", "30");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task SeedUserAsync(
    string email,
    string password,
    bool isActive = true,
    UserRole role = UserRole.Viewer)
    {
        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        var hasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>();

        var normalizedEmail = email.Trim().ToUpperInvariant();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            IsActive = isActive,
            Role = role,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = hasher.HashPassword(user, password);

        db.Users.Add(user);

        await db.SaveChangesAsync();
    }

    public static string SigningSecret => TestSecret;
}
