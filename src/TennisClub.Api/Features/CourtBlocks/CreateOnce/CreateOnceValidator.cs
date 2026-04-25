using FluentValidation;

namespace TennisClub.Api.Features.CourtBlocks.CreateOnce;

public sealed class CreateOnceValidator : AbstractValidator<CreateOnceRequest>
{
    public CreateOnceValidator()
    {
        RuleFor(x => x.CourtId).GreaterThan(0);
        RuleFor(x => x.StartsAt).LessThan(x => x.EndsAt)
            .WithMessage("Ende muss nach dem Start liegen.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
    }
}
