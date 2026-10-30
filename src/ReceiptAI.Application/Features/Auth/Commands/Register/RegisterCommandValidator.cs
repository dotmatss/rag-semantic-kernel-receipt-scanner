using FluentValidation;

namespace ReceiptAI.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public const int MinPasswordLength = 8;

    public RegisterCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(c => c.Password).NotEmpty().MinimumLength(MinPasswordLength).MaximumLength(128);
    }
}
