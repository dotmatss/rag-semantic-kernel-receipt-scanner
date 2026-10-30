using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReceiptAI.Api.Auth;
using ReceiptAI.Application.Auth;

namespace ReceiptAI.Api.Controllers;

public sealed record CredentialsRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register(CredentialsRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.RegisterAsync(request.Email, request.Password, ct);
            return result is null
                ? Conflict(new { error = "Email is already registered." })
                : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login(CredentialsRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password, ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResult>> Refresh(RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshAsync(request.RefreshToken, ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken ct)
    {
        await _authService.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        id = User.GetUserId(),
        email = User.FindFirstValue(JwtRegisteredClaimNames.Email),
        role = User.FindFirstValue(AuthClaims.Role)
    });

    [Authorize(Policy = AuthClaims.AdminPolicy)]
    [HttpGet("~/admin/ping")]
    public IActionResult AdminPing() => Ok(new { message = "pong" });
}
