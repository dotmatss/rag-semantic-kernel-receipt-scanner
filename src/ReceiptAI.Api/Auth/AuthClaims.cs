using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ReceiptAI.Api.Auth;

public static class AuthClaims
{
    public const string Role = "role";

    public const string AdminPolicy = "Admin";

    /// <summary>The signed-in user's id, used as ownerId for repository calls.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
