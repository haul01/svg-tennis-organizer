using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.Rules;

public sealed class NoCourtBlockExistsRule(AppDbContext db) : IBookingRule
{
    public async Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct)
    {
        var blocked = await db.CourtBlocks.AnyAsync(b =>
            b.CourtId == a.CourtId
            && b.StartsAt < a.EndsAt
            && b.EndsAt > a.StartsAt, ct);

        return blocked
            ? RuleResult.Fail("COURT_BLOCKED",
                "Der Platz ist zu dieser Zeit gesperrt.")
            : RuleResult.Ok();
    }
}
