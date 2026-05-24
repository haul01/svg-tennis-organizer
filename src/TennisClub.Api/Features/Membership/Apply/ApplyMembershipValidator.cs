using FluentValidation;

namespace TennisClub.Api.Features.Membership.Apply;

public sealed class ApplyMembershipValidator : AbstractValidator<ApplyMembershipRequest>
{
    public ApplyMembershipValidator(TimeProvider time)
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);

        // Plausibility: born between 1900 and today. The radio buttons on the
        // form already steer adults vs. youth vs. children, so we keep the
        // bounds wide instead of enforcing matching age ranges here.
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        RuleFor(x => x.BirthDate)
            .GreaterThan(new DateOnly(1900, 1, 1))
            .LessThanOrEqualTo(today)
            .WithMessage("Geburtsdatum muss in der Vergangenheit liegen.");

        RuleFor(x => x.FeeTier)
            .NotEmpty()
            .Must(MembershipFeeTiers.IsKnown)
            .WithMessage("Bitte eine gültige Beitragsstufe wählen.");

        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}
