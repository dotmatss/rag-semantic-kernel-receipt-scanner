using ReceiptAI.Application.Auth;
using ReceiptAI.Application.Common.Interfaces.Persistence;
using ReceiptAI.Domain.Entities;

namespace ReceiptAI.Application.Tests;

public class AuthServiceTests
{
    private const string Email = "Jane@Example.com";
    private const string Password = "correct-horse";

    private readonly FakeUserRepository _users = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_users, _refreshTokens, new FakeJwtTokenGenerator());
    }

    [Fact]
    public async Task Register_then_login_returns_tokens()
    {
        Assert.NotNull(await _sut.RegisterAsync(Email, Password));

        var login = await _sut.LoginAsync("  jane@example.COM ", Password);

        Assert.NotNull(login);
        Assert.False(string.IsNullOrEmpty(login.RefreshToken));
        Assert.Equal("jane@example.com", _users.Users.Single().Email);
    }

    [Fact]
    public async Task Register_with_existing_email_returns_null()
    {
        await _sut.RegisterAsync(Email, Password);

        Assert.Null(await _sut.RegisterAsync("jane@example.com", "another-password"));
    }

    [Theory]
    [InlineData("not-an-email", Password)]
    [InlineData(Email, "short")]
    public async Task Register_with_invalid_input_throws(string email, string password)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(email, password));
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_null()
    {
        await _sut.RegisterAsync(Email, Password);

        Assert.Null(await _sut.LoginAsync(Email, "wrong-password"));
        Assert.Null(await _sut.LoginAsync("nobody@example.com", Password));
    }

    [Fact]
    public async Task Refresh_rotates_token_and_old_token_stops_working()
    {
        var registered = await _sut.RegisterAsync(Email, Password);

        var refreshed = await _sut.RefreshAsync(registered!.RefreshToken);

        Assert.NotNull(refreshed);
        Assert.NotEqual(registered.RefreshToken, refreshed.RefreshToken);
        Assert.Null(await _sut.RefreshAsync(registered.RefreshToken));
    }

    [Fact]
    public async Task Reusing_a_revoked_token_revokes_all_sessions()
    {
        var registered = await _sut.RegisterAsync(Email, Password);
        var refreshed = await _sut.RefreshAsync(registered!.RefreshToken);

        await _sut.RefreshAsync(registered.RefreshToken);

        Assert.Null(await _sut.RefreshAsync(refreshed!.RefreshToken));
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected()
    {
        var registered = await _sut.RegisterAsync(Email, Password);
        var stored = _refreshTokens.Tokens.Single();
        _refreshTokens.Tokens[0] = new RefreshToken(stored.UserId, stored.TokenHash, DateTime.UtcNow.AddSeconds(-1));

        Assert.Null(await _sut.RefreshAsync(registered!.RefreshToken));
    }

    [Fact]
    public async Task Logout_revokes_refresh_token()
    {
        var registered = await _sut.RegisterAsync(Email, Password);

        await _sut.LogoutAsync(registered!.RefreshToken);

        Assert.Null(await _sut.RefreshAsync(registered.RefreshToken));
    }

    [Fact]
    public void PasswordHasher_verifies_only_the_original_password()
    {
        var hash = PasswordHasher.Hash(Password);

        Assert.True(PasswordHasher.Verify(Password, hash));
        Assert.False(PasswordHasher.Verify("wrong-password", hash));
        Assert.False(PasswordHasher.Verify(Password, "garbage"));
    }

    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        public (string Token, DateTime ExpiresAt) Generate(User user) =>
            ($"access-{user.Id}", DateTime.UtcNow.AddMinutes(15));
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
