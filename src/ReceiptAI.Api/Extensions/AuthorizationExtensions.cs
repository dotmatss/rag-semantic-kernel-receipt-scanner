using System;
using Microsoft.AspNetCore.Authorization;
using ReceiptAI.Domain.Enums;

namespace ReceiptAI.Api.Extensions;

public static class Policies
{
    public const string AdminOnly = nameof(AdminOnly);
    public const string User = nameof(User);
}

public static class Roles
{
    public const string Admin = nameof(UserRole.Admin);
    public const string User = nameof(UserRole.User);
}

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(Policies.User, policy => policy.RequireRole(Roles.User));

        return services;
    }

}
