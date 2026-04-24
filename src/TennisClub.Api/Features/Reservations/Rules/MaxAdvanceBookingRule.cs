using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.Rules;

public sealed class MaxAdvanceBookingRule(AppDbContext db, TimeProvider time) : IBookingRule
{
    public async Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct)
    {
        var settings = await db.SystemSettings.AsNoTracking().FirstAsync(ct);
        var maxDate = time.GetUtcNow().AddDays(settings.MaxAdvanceBookingDays);

        return a.StartsAt <= maxDate
            ? RuleResult.Ok()
            : RuleResult.Fail("TOO_FAR",
                $"Buchungen sind nur bis {settings.MaxAdvanceBookingDays} Tage im Voraus möglich.");
    }
}
