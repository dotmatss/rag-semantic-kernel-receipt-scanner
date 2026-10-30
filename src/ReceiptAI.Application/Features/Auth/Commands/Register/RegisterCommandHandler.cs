using MediatR;
using ReceiptAI.Application.Common.Exceptions;
using ReceiptAI.Application.Common.Interfaces.Auth;
using ReceiptAI.Application.Common.Interfaces.Persistence;
using ReceiptAI.Application.Common.Models;
using ReceiptAI.Domain.Entities;

namespace ReceiptAI.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IJwtService jwt) : IRequestHandler<RegisterCommand, JwtResult>
{
    public async Task<JwtResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await users.GetByEmailAsync(User.NormalizeEmail(request.Email), cancellationToken) is not null)
            throw new ConflictException("Email is already registered.");

        var user = new User(request.Email, passwordHasher.HashPassword(request.Password));
        await users.AddAsync(user, cancellationToken);

        return await jwt.CreateTokensAsync(user, cancellationToken);
    }
}
