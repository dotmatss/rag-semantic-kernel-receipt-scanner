using MediatR;
using ReceiptAI.Application.Common.Interfaces.Auth;

namespace ReceiptAI.Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest;

public class LogoutCommandHandler(IJwtService jwt) : IRequestHandler<LogoutCommand>
{
    public Task Handle(LogoutCommand request, CancellationToken cancellationToken) =>
        jwt.RevokeAsync(request.RefreshToken, cancellationToken);
}
