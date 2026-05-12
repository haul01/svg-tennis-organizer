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

        var totalMinutes = (a.EndsAt - a.StartsAt).TotalMinutes;
        var slot = season.SlotDurationMinutes;

        // The duration must be a positive integer multiple of the slot
        // length - half a minute tolerance covers DST seconds drift.
        var ratio = totalMinutes / slot;
        var slotCount = (int)Math.Round(ratio);
        if (slotCount < 1 || Math.Abs(totalMinutes - slotCount * slot) > 0.5)
        {
            return RuleResult.Fail(
                "INVALID_DURATION",
                $"Die Buchungsdauer muss ein Vielfaches von {slot} Minuten sein.");
        }

        var settings = await db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var maxSlots = settings?.MaxSlotsPerBooking ?? 4;
        if (slotCount > maxSlots)
        {
            return RuleResult.Fail(
                "INVALID_DURATION",
                $"Maximal {maxSlots} aufeinanderfolgende Slots pro Buchung ({maxSlots * slot} Minuten).");
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
