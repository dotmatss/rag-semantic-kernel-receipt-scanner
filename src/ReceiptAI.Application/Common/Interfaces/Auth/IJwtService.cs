using ReceiptAI.Application.Common.Models;
using ReceiptAI.Domain.Entities;

namespace ReceiptAI.Application.Common.Interfaces.Auth;

public interface IJwtService
{
    Task<JwtResult> CreateTokensAsync(User user, CancellationToken cancellationToken);

    /// <summary>Rotates the refresh token. Throws UnauthorizedException when it is unknown, expired or revoked.</summary>
    Task<JwtResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);
}
