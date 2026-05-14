using FluentValidation;

namespace TennisClub.Api.Features.CourtBlocks.CreateSeries;

public sealed class CreateSeriesValidator : AbstractValidator<CreateSeriesRequest>
{
    public CreateSeriesValidator()
    {
        // CourtId only required when targeting a single court; the
        // all-courts path resolves them server-side.
        RuleFor(x => x.CourtId)
            .GreaterThan(0)
            .When(x => !x.AllCourts);
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Gültig-bis-Datum muss am oder nach dem Gültig-ab-Datum liegen.");
        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("Ende-Uhrzeit muss nach der Start-Uhrzeit liegen.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
    }
}
