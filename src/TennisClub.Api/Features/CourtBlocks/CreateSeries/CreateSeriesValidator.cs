using FluentValidation;

namespace TennisClub.Api.Features.CourtBlocks.CreateSeries;

public sealed class CreateSeriesValidator : AbstractValidator<CreateSeriesRequest>
{
    public CreateSeriesValidator()
    {
        RuleFor(x => x.CourtId).GreaterThan(0);
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Gültig-bis-Datum muss am oder nach dem Gültig-ab-Datum liegen.");
        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("Ende-Uhrzeit muss nach der Start-Uhrzeit liegen.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
    }
}
