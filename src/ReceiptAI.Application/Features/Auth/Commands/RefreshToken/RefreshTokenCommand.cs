using MediatR;
using ReceiptAI.Application.Common.Interfaces.Auth;
using ReceiptAI.Application.Common.Models;

namespace ReceiptAI.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<JwtResult>;

public class RefreshTokenCommandHandler(IJwtService jwt) : IRequestHandler<RefreshTokenCommand, JwtResult>
{
    public Task<JwtResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken) =>
        jwt.RefreshAsync(request.RefreshToken, cancellationToken);
}
