namespace ReceiptAI.Application.Common.Models;

public sealed record JwtResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
