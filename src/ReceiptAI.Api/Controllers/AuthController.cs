using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReceiptAI.Api.Extensions;
using ReceiptAI.Application.Common.Models;
using ReceiptAI.Application.Features.Auth.Commands.Login;
using ReceiptAI.Application.Features.Auth.Commands.Logout;
using ReceiptAI.Application.Features.Auth.Commands.RefreshToken;
using ReceiptAI.Application.Features.Auth.Commands.Register;
using ReceiptAI.Application.Features.Auth.Queries.GetCurrentUser;

namespace ReceiptAI.Api.Controllers;

public sealed record RefreshTokenRequest(string RefreshToken);

[Route("api/[controller]")]
[ApiController]
public class AuthController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public Task<JwtResult> Register(RegisterCommand command, CancellationToken ct) => sender.Send(command, ct);

    [AllowAnonymous]
    [HttpPost("login")]
    public Task<JwtResult> Login(LoginCommand command, CancellationToken ct) => sender.Send(command, ct);

    [AllowAnonymous]
    [HttpPost("refresh")]
    public Task<JwtResult> Refresh(RefreshTokenRequest request, CancellationToken ct) =>
        sender.Send(new RefreshTokenCommand(request.RefreshToken), ct);

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken ct)
    {
        await sender.Send(new LogoutCommand(request.RefreshToken), ct);
        return NoContent();
    }

    [HttpGet("me")]
    public Task<CurrentUserResponse> Me(CancellationToken ct) => sender.Send(new GetCurrentUserQuery(), ct);

    [Authorize(Policy = Policies.AdminOnly)]
    [HttpGet("admin")]
    public IActionResult AdminOnly() => Ok(new { message = "Hello, admin." });
}
