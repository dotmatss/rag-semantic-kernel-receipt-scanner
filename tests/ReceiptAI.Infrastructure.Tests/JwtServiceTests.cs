using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using ReceiptAI.Application.Common.Exceptions;
using ReceiptAI.Application.Common.Interfaces.Persistence;
using ReceiptAI.Domain.Entities;
using ReceiptAI.Domain.Enums;
using ReceiptAI.Infrastructure.Auth;

namespace ReceiptAI.Infrastructure.Tests;

public class JwtServiceTests
{
    private readonly User _user = new("jane@example.com", "hash", UserRole.Admin);
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly JwtService _sut;

    public JwtServiceTests()
    {
        var options = Options.Create(new JwtOptions
        {
            Key = "test-secret-that-is-at-least-32-bytes-long",
            Issuer = "ReceiptAI",
            Audience = "ReceiptAI.Client"
        });

        var users = new FakeUserRepository();
        users.Users.Add(_user);

        _sut = new JwtService(_refreshTokens, users, options);
    }

    [Fact]
    public async Task Access_token_carries_user_claims()
    {
        var tokens = await _sut.CreateTokensAsync(_user, default);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        Assert.Equal(_user.Id.ToString(), jwt.Subject);
        Assert.Equal("Admin", jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal("ReceiptAI", jwt.Issuer);
        Assert.Equal("HS256", jwt.Header.Alg);
    }

    [Fact]
    public async Task Only_the_refresh_token_hash_is_stored()
    {
        var tokens = await _sut.CreateTokensAsync(_user, default);

        var stored = _refreshTokens.Tokens.Single();
        Assert.NotEqual(tokens.RefreshToken, stored.TokenHash);
        Assert.Equal(64, stored.TokenHash.Length);
    }

    [Fact]
    public async Task Refresh_rotates_token_and_old_token_stops_working()
    {
        var created = await _sut.CreateTokensAsync(_user, default);

        var refreshed = await _sut.RefreshAsync(created.RefreshToken, default);

        Assert.NotEqual(created.RefreshToken, refreshed.RefreshToken);
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.RefreshAsync(created.RefreshToken, default));
    }

    [Fact]
    public async Task Reusing_a_revoked_token_revokes_all_sessions()
    {
        var created = await _sut.CreateTokensAsync(_user, default);
        var refreshed = await _sut.RefreshAsync(created.RefreshToken, default);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.RefreshAsync(created.RefreshToken, default));

        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.RefreshAsync(refreshed.RefreshToken, default));
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected()
    {
        var created = await _sut.CreateTokensAsync(_user, default);
        var stored = _refreshTokens.Tokens.Single();
        _refreshTokens.Tokens[0] = new RefreshToken(stored.UserId, stored.TokenHash, DateTime.UtcNow.AddSeconds(-1));

        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.RefreshAsync(created.RefreshToken, default));
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown-token")]
    public async Task Unknown_refresh_token_is_rejected(string refreshToken)
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.RefreshAsync(refreshToken, default));
    }

    [Fact]
    public async Task Revoked_refresh_token_cannot_be_used()
    {
        var created = await _sut.CreateTokensAsync(_user, default);

        await _sut.RevokeAsync(created.RefreshToken, default);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.RefreshAsync(created.RefreshToken, default));
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = new();

        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Id == userId));

        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Email == normalizedEmail));

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<RefreshToken> Tokens { get; } = new();

        public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            Tokens.Add(refreshToken);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            foreach (var token in Tokens.Where(t => t.UserId == userId))
                token.Revoke();

            return Task.CompletedTask;
        }
    }
}
