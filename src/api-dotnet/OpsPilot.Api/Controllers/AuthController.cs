using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsPilot.Api.Contracts;
using OpsPilot.Api.Security;
using OpsPilot.Application.Authentication;

namespace OpsPilot.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        IAuthService authService,
        JwtTokenService jwtTokenService)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _authService.AuthenticateAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (user is null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var (token, expiresAtUtc) =
            _jwtTokenService.GenerateToken(user);

        return Ok(new LoginResponse(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresAtUtc: expiresAtUtc
        ));
    }
}