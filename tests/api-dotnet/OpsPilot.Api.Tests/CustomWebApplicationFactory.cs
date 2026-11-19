using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Identity;
using OpsPilot.Domain.Entities;
using OpsPilot.Infrastructure.Persistence;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace OpsPilot.Api.Tests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private const string TestSecret =
        "OpsPilot-Integration-Test-Secret-1234567890";

    private readonly string _databaseName =
        $"OpsPilotTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Database=opspilot_tests;Username=test;Password=test",

                ["Jwt:Secret"] = TestSecret,
                ["Jwt:Issuer"] = "OpsPilot.Tests",
                ["Jwt:Audience"] = "OpsPilot.Tests.Client",
                ["Jwt:AccessTokenMinutes"] = "30"
            };

            config.AddInMemoryCollection(settings);
        });

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
        bool isActive = true)
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
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = hasher.HashPassword(user, password);

        db.Users.Add(user);

        await db.SaveChangesAsync();
    }

    public static string SigningSecret => TestSecret;
}