using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using ReceiptAI.Application.Common.Interfaces.Persistence;
using ReceiptAI.Domain.Entities;

namespace ReceiptAI.Application.Auth;

public class AuthService
{
    public const int MinPasswordLength = 8;

    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14);

    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtTokenGenerator _jwt;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IJwtTokenGenerator jwt)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _jwt = jwt;
    }

    /// <returns>null when the email is already registered.</returns>
    public async Task<AuthResult?> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!MailAddress.TryCreate(email, out _))
            throw new ArgumentException("A valid email is required.");

        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
            throw new ArgumentException($"Password must be at least {MinPasswordLength} characters.");

        if (await _users.GetByEmailAsync(User.NormalizeEmail(email), cancellationToken) is not null)
            return null;

        var user = new User(email, PasswordHasher.Hash(password));
        await _users.AddAsync(user, cancellationToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    /// <returns>null when the credentials are invalid.</returns>
    public async Task<AuthResult?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            return null;

        var user = await _users.GetByEmailAsync(User.NormalizeEmail(email), cancellationToken);
        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
            return null;

        return await IssueTokensAsync(user, cancellationToken);
    }

    /// <summary>Rotates the refresh token: the presented one is revoked and a new pair is issued.</summary>
    /// <returns>null when the refresh token is unknown, expired or revoked.</returns>
    public async Task<AuthResult?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var stored = await _refreshTokens.GetByHashAsync(HashToken(refreshToken), cancellationToken);
        if (stored is null)
            return null;

        if (!stored.IsActive)
        {
            // A revoked token being replayed may be stolen, so end every session of that user.
            if (stored.RevokedAt is not null)
                await _refreshTokens.RevokeAllForUserAsync(stored.UserId, cancellationToken);

            return null;
        }

        var user = await _users.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null)
            return null;

        stored.Revoke();
        await _refreshTokens.UpdateAsync(stored, cancellationToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var stored = await _refreshTokens.GetByHashAsync(HashToken(refreshToken), cancellationToken);
        if (stored is null || !stored.IsActive)
            return;

        stored.Revoke();
        await _refreshTokens.UpdateAsync(stored, cancellationToken);
    }

    private async Task<AuthResult> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var (accessToken, accessExpiresAt) = _jwt.Generate(user);

        var refreshToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var refreshExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime);
        await _refreshTokens.AddAsync(
            new RefreshToken(user.Id, HashToken(refreshToken), refreshExpiresAt),
            cancellationToken);

        return new AuthResult(accessToken, accessExpiresAt, refreshToken, refreshExpiresAt);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
