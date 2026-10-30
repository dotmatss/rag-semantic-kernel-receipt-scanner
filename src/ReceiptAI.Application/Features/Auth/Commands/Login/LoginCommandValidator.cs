using FluentValidation;

namespace ReceiptAI.Application.Features.Auth.Commands.Login;

// Only shape checks: format and password-policy rules belong to registration, so existing users can always log in.
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().MaximumLength(320);
        RuleFor(c => c.Password).NotEmpty().MaximumLength(128);
    }
}
