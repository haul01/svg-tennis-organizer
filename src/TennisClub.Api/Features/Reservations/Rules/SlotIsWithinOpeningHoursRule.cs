using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.Rules;

public sealed class SlotIsWithinOpeningHoursRule(AppDbContext db, TimeProvider time) : IBookingRule
{
    public async Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct)
    {
        var season = await SlotBoundsAreValidRule.FindActiveSeasonAsync(db, time, ct);
        // No active season is reported by SlotIsWithinSeasonRule; stay silent here.
        if (season is null) return RuleResult.Ok();

        var startTime = TimeOnly.FromDateTime(a.StartsAt.DateTime);
        var endTime = TimeOnly.FromDateTime(a.EndsAt.DateTime);

        if (startTime < season.OpeningTime || endTime > season.ClosingTime)
        {
            return RuleResult.Fail(
                "OUTSIDE_HOURS",
                $"Der Slot liegt außerhalb der Öffnungszeiten ({season.OpeningTime:HH\\:mm}–{season.ClosingTime:HH\\:mm}).");
        }

        return RuleResult.Ok();
    }
}
