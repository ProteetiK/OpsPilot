
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpsPilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using OpsPilot.Application.Authentication;
using OpsPilot.Domain.Entities;
using OpsPilot.Infrastructure.Authentication;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpsPilot.Api.Security;
using OpsPilot.Infrastructure.Authentication;
using OpsPilot.Application.Users;
using OpsPilot.Infrastructure.Users;
using OpsPilot.Application.Incidents;
using OpsPilot.Infrastructure.Incidents;
using OpsPilot.Application.Services;
using OpsPilot.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.Secret) ||
    Encoding.UTF8.GetByteCount(jwtOptions.Secret) < 32)
{
    throw new InvalidOperationException(
        "JWT secret must contain at least 32 bytes.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException(
        "JWT issuer and audience must be configured.");
}

if (jwtOptions.AccessTokenMinutes <= 0 ||
    jwtOptions.AccessTokenMinutes > 1440)
{
    throw new InvalidOperationException(
        "JWT access-token lifetime must be between 1 and 1440 minutes.");
}

builder.Services.AddSingleton(jwtOptions);

builder.Services.AddSingleton<JwtTokenService>();

// 1. Read database connection string from environment variables
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string is missing.");
}

// 2. Register PostgreSQL database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Authentication

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

//Incidents
builder.Services.AddScoped<IIncidentService, IncidentService>();

//Service
builder.Services.AddScoped<IServiceManagement, ServiceManagement>();

// 3. Register health checks
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(
        name: "postgres",
        tags: new[] { "ready" });

// 4. Register OpenAPI
builder.Services.AddOpenApi();

//5. Register JWT
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Secret)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            NameClaimType = "email"
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Engineers and Administrators can investigate and update incidents.
    options.AddPolicy("IncidentInvestigation", policy =>
        policy.RequireRole("Engineer", "Administrator"));

    options.AddPolicy("IncidentUpdate", policy =>
        policy.RequireRole("Engineer", "Administrator"));

    // Only Administrators can manage services and users.
    options.AddPolicy("ManageServices", policy =>
        policy.RequireRole("Administrator"));

    options.AddPolicy("ManageUsers", policy =>
        policy.RequireRole("Administrator"));

    options.AddPolicy("AssignRoles", policy =>
        policy.RequireRole("Administrator"));
});
// 5. Build application
builder.Services.AddControllers();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await DemoUserSeeder.SeedAsync(
        app.Services,
        app.Configuration);
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 6. Configure OpenAPI in Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 7. Liveness endpoint
app.MapGet("/health/live", () =>
{
    return Results.Ok(new
    {
        status = "Alive"
    });
});

// 8. Readiness endpoint - checks PostgreSQL
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// 9. HTTPS redirection
app.UseHttpsRedirection();

// 10. Weather forecast endpoint
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild",
    "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

    return forecast;
})
.WithName("GetWeatherForecast");

// 11. Start application
app.Run();

public partial class Program { }

// Weather forecast model
record WeatherForecast(
    DateOnly Date,
    int TemperatureC,
    string? Summary)
{
    public int TemperatureF =>
        32 + (int)(TemperatureC / 0.5556);
}
