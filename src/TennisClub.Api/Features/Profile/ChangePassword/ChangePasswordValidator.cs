using FluentValidation;

namespace TennisClub.Api.Features.Profile.ChangePassword;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(6).WithMessage("Das Passwort muss mindestens 6 Zeichen lang sein.")
            .NotEqual(x => x.CurrentPassword)
                .WithMessage("Das neue Passwort muss sich vom aktuellen unterscheiden.");
    }
}
