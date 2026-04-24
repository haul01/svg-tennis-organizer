using FluentValidation;

namespace TennisClub.Api.Features.Seasons.Update;

public sealed class UpdateSeasonValidator : AbstractValidator<UpdateSeasonRequest>
{
    public UpdateSeasonValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Ende-Datum muss am oder nach dem Start-Datum liegen.");

        RuleFor(x => x.ClosingTime)
            .GreaterThan(x => x.OpeningTime)
            .WithMessage("Schlusszeit muss nach der Öffnungszeit liegen.");

        RuleFor(x => x.SlotDurationMinutes)
            .InclusiveBetween(15, 240)
            .WithMessage("Slot-Dauer muss zwischen 15 und 240 Minuten liegen.");
    }
}
