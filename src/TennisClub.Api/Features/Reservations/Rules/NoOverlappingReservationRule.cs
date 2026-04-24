using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.Rules;

public sealed class NoOverlappingReservationRule(AppDbContext db) : IBookingRule
{
    public async Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct)
    {
        var exists = await db.Reservations.AnyAsync(r =>
            r.CourtId == a.CourtId
            && r.Status == ReservationStatus.Active
            && r.StartsAt < a.EndsAt
            && r.EndsAt > a.StartsAt, ct);

        return exists
            ? RuleResult.Fail("OVERLAP",
                "Der Platz ist zu dieser Zeit bereits gebucht.")
            : RuleResult.Ok();
    }
}
