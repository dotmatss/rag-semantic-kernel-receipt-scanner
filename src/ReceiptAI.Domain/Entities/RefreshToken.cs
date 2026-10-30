using ReceiptAI.Domain.Common;

namespace ReceiptAI.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }

    // Only the SHA-256 hash is stored, so a database leak does not leak usable tokens.
    public string TokenHash { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.");

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash is required.");

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public void Revoke()
    {
        if (RevokedAt is not null)
            return;

        RevokedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
