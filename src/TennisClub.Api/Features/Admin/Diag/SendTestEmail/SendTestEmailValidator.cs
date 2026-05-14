using FluentValidation;

namespace TennisClub.Api.Features.Admin.Diag.SendTestEmail;

public sealed class SendTestEmailValidator : AbstractValidator<SendTestEmailRequest>
{
    public SendTestEmailValidator()
    {
        RuleFor(x => x.To)
            .NotEmpty().WithMessage("Empfänger-Adresse fehlt.")
            .EmailAddress().WithMessage("Empfänger-Adresse ist keine gültige E-Mail.");
    }
}
