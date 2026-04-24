using FluentValidation;

namespace TennisClub.Api.Features.Settings.Admin;

public sealed class UpdateSettingsValidator : AbstractValidator<UpdateSettingsRequest>
{
    public UpdateSettingsValidator()
    {
        RuleFor(x => x.MaxAdvanceBookingDays)
            .InclusiveBetween(1, 365)
            .WithMessage("Vorlauf muss zwischen 1 und 365 Tagen liegen.");

        RuleFor(x => x.MinCancellationHours)
            .InclusiveBetween(0, 168)
            .WithMessage("Stornofrist muss zwischen 0 und 168 Stunden liegen.");

        RuleFor(x => x.MaxOpenReservationsPerMember)
            .InclusiveBetween(1, 20)
            .WithMessage("Offene Buchungen pro Mitglied müssen zwischen 1 und 20 liegen.");
    }
}
