using MediatR;
using ReceiptAI.Application.Common.Models;

namespace ReceiptAI.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(string Email, string Password) : IRequest<JwtResult>;
