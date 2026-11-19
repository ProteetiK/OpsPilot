using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OpsPilot.Api.Contracts;

namespace OpsPilot.Api.Tests;

public class AuthenticationTests
{
    private const string TestEmail = "test@opspilot.local";
    private const string TestPassword = "Test-Password-123!";

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(TestEmail, TestPassword);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = TestEmail,
                password = TestPassword
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.Equal("Bearer", result.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_Returns401()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(TestEmail, TestPassword);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = TestEmail,
                password = "Wrong-Password-123!"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "unknown@opspilot.local",
                password = TestPassword
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_Returns401()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                CustomWebApplicationFactory.SigningSecret));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "OpsPilot.Tests",
            audience: "OpsPilot.Tests.Client",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())
            },
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenString);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInactiveUser_Returns401()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.SeedUserAsync(
            TestEmail,
            TestPassword,
            isActive: false);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = TestEmail,
                password = TestPassword
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidSignature_Returns401()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var wrongKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                "Completely-Different-Test-Secret-123456789"));

        var credentials = new SigningCredentials(
            wrongKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "OpsPilot.Tests",
            audience: "OpsPilot.Tests.Client",
            claims: new[]
            {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                Guid.NewGuid().ToString())
            },
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenString);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}