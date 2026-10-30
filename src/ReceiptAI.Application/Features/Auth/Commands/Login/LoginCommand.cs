using MediatR;
using ReceiptAI.Application.Common.Models;

namespace ReceiptAI.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<JwtResult>;
