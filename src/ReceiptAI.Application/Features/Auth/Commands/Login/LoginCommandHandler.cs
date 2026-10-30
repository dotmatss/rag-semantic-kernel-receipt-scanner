using MediatR;
using ReceiptAI.Application.Common.Exceptions;
using ReceiptAI.Application.Common.Interfaces.Auth;
using ReceiptAI.Application.Common.Interfaces.Persistence;
using ReceiptAI.Application.Common.Models;
using ReceiptAI.Domain.Entities;

namespace ReceiptAI.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IJwtService jwt) : IRequestHandler<LoginCommand, JwtResult>
{
    public async Task<JwtResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            throw new UnauthorizedException("Invalid email or password.");

        var user = await users.GetByEmailAsync(User.NormalizeEmail(request.Email), cancellationToken);

        // Same error for unknown email and wrong password, so emails can't be enumerated.
        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        return await jwt.CreateTokensAsync(user, cancellationToken);
    }
}
