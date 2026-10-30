using MediatR;
using ReceiptAI.Application.Common.Exceptions;
using ReceiptAI.Application.Common.Interfaces;
using ReceiptAI.Application.Common.Interfaces.Persistence;

namespace ReceiptAI.Application.Features.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<CurrentUserResponse>;

public sealed record CurrentUserResponse(Guid Id, string Email, string Role);

public class GetCurrentUserQueryHandler(ICurrentUser currentUser, IUserRepository users)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    public async Task<CurrentUserResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedException();

        // A valid token for a deleted user is still unauthorized.
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException();

        return new CurrentUserResponse(user.Id, user.Email, user.Role.ToString());
    }
}
