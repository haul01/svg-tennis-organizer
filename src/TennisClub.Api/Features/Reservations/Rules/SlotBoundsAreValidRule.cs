using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.Rules;

public sealed class SlotBoundsAreValidRule(AppDbContext db, TimeProvider time) : IBookingRule
{
    public async Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct)
    {
        if (a.StartsAt >= a.EndsAt)
        {
            return RuleResult.Fail(
                "INVALID_BOUNDS",
                "Start-Zeitpunkt muss vor End-Zeitpunkt liegen.");
        }

        var season = await FindActiveSeasonAsync(db, time, ct);
        if (season is null) return RuleResult.Ok();

        var duration = a.EndsAt - a.StartsAt;
        if (Math.Abs(duration.TotalMinutes - season.SlotDurationMinutes) > 0.5)
        {
            return RuleResult.Fail(
                "INVALID_DURATION",
                $"Ein Slot muss genau {season.SlotDurationMinutes} Minuten dauern.");
        }

        return RuleResult.Ok();
    }

    internal static Task<Domain.Entities.Season?> FindActiveSeasonAsync(
        AppDbContext db, TimeProvider time, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        return db.Seasons
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StartDate <= today && s.EndDate >= today, ct);
    }
}
