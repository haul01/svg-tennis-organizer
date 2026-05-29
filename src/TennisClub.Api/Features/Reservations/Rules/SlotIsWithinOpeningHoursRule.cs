using TennisClub.Api.Common.Time;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.Rules;

public sealed class SlotIsWithinOpeningHoursRule(AppDbContext db, TimeProvider time) : IBookingRule
{
    public async Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct)
    {
        var season = await SlotBoundsAreValidRule.FindActiveSeasonAsync(db, time, ct);
        // No active season is reported by SlotIsWithinSeasonRule; stay silent here.
        if (season is null) return RuleResult.Ok();

        // OpeningTime / ClosingTime are TimeOnly without TZ - compare against
        // Vienna wall-clock so a 09:00 Vienna booking matches an 08:00-22:00
        // window even though the JSON arrives as 07:00 UTC.
        //
        // Compare full local DateTimes, not just time-of-day: a multi-slot
        // booking running past local midnight (e.g. 21:00-01:00) would
        // otherwise have its end wrap to 01:00, which is < ClosingTime, and
        // slip through - booking the court for hours after closing.
        var localStart = ClubTimeZone.LocalDateTime(a.StartsAt);
        var localEnd = ClubTimeZone.LocalDateTime(a.EndsAt);
        var openingInstant = localStart.Date + season.OpeningTime.ToTimeSpan();
        var closingInstant = localStart.Date + season.ClosingTime.ToTimeSpan();

        if (localStart < openingInstant || localEnd > closingInstant)
        {
            return RuleResult.Fail(
                "OUTSIDE_HOURS",
                $"Der Slot liegt außerhalb der Öffnungszeiten ({season.OpeningTime:HH\\:mm}–{season.ClosingTime:HH\\:mm}).");
        }

        return RuleResult.Ok();
    }
}
