using System;
using ReceiptAI.Application;

namespace ReceiptAI.Infrastructure;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // A malformed stored hash must fail login, not crash it.
            return false;
        }
    }
}
