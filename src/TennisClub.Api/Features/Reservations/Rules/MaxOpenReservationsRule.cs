using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.Rules;

public sealed class MaxOpenReservationsRule(AppDbContext db, TimeProvider time) : IBookingRule
{
    public async Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct)
    {
        var settings = await db.SystemSettings.AsNoTracking().FirstAsync(ct);
        var now = time.GetUtcNow();

        var openCount = await db.Reservations
            .Where(r => r.MemberId == a.MemberId
                && r.Status == ReservationStatus.Active
                && r.StartsAt > now)
            .CountAsync(ct);

        return openCount < settings.MaxOpenReservationsPerMember
            ? RuleResult.Ok()
            : RuleResult.Fail("TOO_MANY_OPEN",
                $"Du hast bereits {settings.MaxOpenReservationsPerMember} offene Buchungen.");
    }
}
