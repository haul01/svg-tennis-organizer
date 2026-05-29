using FluentValidation;

namespace TennisClub.Api.Features.Auth.ResetPassword;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        // de-AT messages: this text is surfaced to the user on the
        // set-password screen, so it must not fall back to FluentValidation's
        // English defaults.
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Bitte ein Passwort eingeben.")
            .MinimumLength(6).WithMessage("Das Passwort muss mindestens 6 Zeichen lang sein.");
    }
}
