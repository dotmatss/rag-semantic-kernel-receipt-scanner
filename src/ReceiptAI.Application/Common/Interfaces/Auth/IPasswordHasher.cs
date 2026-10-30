using System;

namespace ReceiptAI.Application;

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string hashedPassword);

}
