using ReceiptAI.Domain.Common;
using ReceiptAI.Domain.Enums;

namespace ReceiptAI.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    private User()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(string email, string passwordHash, UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.");

        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        Role = role;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
