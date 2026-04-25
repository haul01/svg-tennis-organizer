using TennisClub.Api.Common.Time;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.Rules;

public sealed class SlotIsWithinSeasonRule(AppDbContext db, TimeProvider time) : IBookingRule
{
    public async Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct)
    {
        var season = await SlotBoundsAreValidRule.FindActiveSeasonAsync(db, time, ct);
        if (season is null)
        {
            return RuleResult.Fail(
                "NO_SEASON",
                "Zurzeit ist keine Saison aktiv. Buchungen sind nicht möglich.");
        }

        // Compare against Vienna wall-clock - the season's StartDate / EndDate
        // are stored as DateOnly without TZ context.
        var slotDate = ClubTimeZone.LocalDate(a.StartsAt);
        if (slotDate < season.StartDate || slotDate > season.EndDate)
        {
            return RuleResult.Fail(
                "OUTSIDE_SEASON",
                "Der Slot liegt außerhalb der aktiven Saison.");
        }

        return RuleResult.Ok();
    }
}
