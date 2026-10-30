using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ReceiptAI.Application.Common.Exceptions;
using ReceiptAI.Application.Common.Interfaces.Auth;
using ReceiptAI.Application.Common.Interfaces.Persistence;
using ReceiptAI.Application.Common.Models;
using ReceiptAI.Domain.Entities;

namespace ReceiptAI.Infrastructure.Auth;

public class JwtService(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    IOptions<JwtOptions> options) : IJwtService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<JwtResult> CreateTokensAsync(User user, CancellationToken cancellationToken)
    {
        var accessExpiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var accessToken = CreateAccessToken(user, accessExpiresAt);

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);
        await refreshTokens.AddAsync(
            new RefreshToken(user.Id, Hash(refreshToken), refreshExpiresAt),
            cancellationToken);

        return new JwtResult(accessToken, accessExpiresAt, refreshToken, refreshExpiresAt);
    }

    public async Task<JwtResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var stored = await FindAsync(refreshToken, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (!stored.IsActive)
        {
            // A revoked token being replayed may be stolen, so end every session of that user.
            if (stored.RevokedAt is not null)
                await refreshTokens.RevokeAllForUserAsync(stored.UserId, cancellationToken);

            throw new UnauthorizedException("Invalid refresh token.");
        }

        var user = await users.GetByIdAsync(stored.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        stored.Revoke();
        await refreshTokens.UpdateAsync(stored, cancellationToken);

        return await CreateTokensAsync(user, cancellationToken);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var stored = await FindAsync(refreshToken, cancellationToken);
        if (stored is null || !stored.IsActive)
            return;

        stored.Revoke();
        await refreshTokens.UpdateAsync(stored, cancellationToken);
    }

    private string CreateAccessToken(User user, DateTime expiresAt)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("role", user.Role.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private Task<RefreshToken?> FindAsync(string refreshToken, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(refreshToken)
            ? Task.FromResult<RefreshToken?>(null)
            : refreshTokens.GetByHashAsync(Hash(refreshToken), cancellationToken);

    // Refresh tokens are high-entropy random values, so a fast unsalted SHA-256 is enough to store them.
    private static string Hash(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
