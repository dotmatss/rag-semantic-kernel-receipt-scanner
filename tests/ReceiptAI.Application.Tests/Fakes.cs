using ReceiptAI.Application.Common.Interfaces;
using ReceiptAI.Application.Common.Interfaces.Auth;
using ReceiptAI.Application.Common.Interfaces.Persistence;
using ReceiptAI.Application.Common.Models;
using ReceiptAI.Domain.Entities;

namespace ReceiptAI.Application.Tests;

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) => $"hashed:{password}";

    public bool VerifyPassword(string password, string hash) => hash == HashPassword(password);
}

internal sealed class FakeJwtService : IJwtService
{
    public Task<JwtResult> CreateTokensAsync(User user, CancellationToken ct) =>
        Task.FromResult(new JwtResult($"access-{user.Id}", DateTime.UtcNow, "refresh", DateTime.UtcNow));

    public Task<JwtResult> RefreshAsync(string refreshToken, CancellationToken ct) =>
        Task.FromResult(new JwtResult("access", DateTime.UtcNow, $"rotated-{refreshToken}", DateTime.UtcNow));

    public Task RevokeAsync(string refreshToken, CancellationToken ct) => Task.CompletedTask;
}

internal sealed class FakeUserRepository : IUserRepository
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

internal sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; init; }
}
