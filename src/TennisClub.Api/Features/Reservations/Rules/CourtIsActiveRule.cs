using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.Rules;

public sealed class CourtIsActiveRule(AppDbContext db) : IBookingRule
{
    public async Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct)
    {
        var court = await db.Courts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == a.CourtId, ct);

        if (court is null)
            return RuleResult.Fail("COURT_UNKNOWN", "Der Platz existiert nicht.");

        if (!court.IsActive)
            return RuleResult.Fail("COURT_INACTIVE", "Der Platz ist nicht buchbar.");

        return RuleResult.Ok();
    }
}
