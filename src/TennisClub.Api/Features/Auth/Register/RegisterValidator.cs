using FluentValidation;

namespace TennisClub.Api.Features.Auth.Register;

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-Mail-Adresse fehlt.")
            .EmailAddress().WithMessage("E-Mail-Adresse ist ungültig.")
            .MaximumLength(256);

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Vorname fehlt.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Nachname fehlt.")
            .MaximumLength(100);
    }
}
