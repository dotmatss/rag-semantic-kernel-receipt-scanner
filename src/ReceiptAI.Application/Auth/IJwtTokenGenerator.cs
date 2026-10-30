using ReceiptAI.Domain.Entities;

namespace ReceiptAI.Application.Auth;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) Generate(User user);
}
