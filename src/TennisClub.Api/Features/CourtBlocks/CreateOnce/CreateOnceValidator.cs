using FluentValidation;

namespace TennisClub.Api.Features.CourtBlocks.CreateOnce;

public sealed class CreateOnceValidator : AbstractValidator<CreateOnceRequest>
{
    public CreateOnceValidator()
    {
        // CourtId only required when sperring a single court; AllCourts
        // mode picks the courts list up server-side.
        RuleFor(x => x.CourtId)
            .GreaterThan(0)
            .When(x => !x.AllCourts);
        RuleFor(x => x.StartsAt).LessThan(x => x.EndsAt)
            .WithMessage("Ende muss nach dem Start liegen.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
    }
}
