using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Infrastructure.Persistence;
using TennisClub.Api.Infrastructure.Persistence.Seed;

namespace TennisClub.Api.Features.Reservations.Rules;

/// <summary>
/// Guests may only book courts that the admin explicitly opted in
/// (typically Platz 3/4). Non-guest roles (Member, Trainer, Admin)
/// short-circuit at the role check.
/// </summary>
public sealed class CourtAllowsGuestRule(AppDbContext db) : IBookingRule
{
    public async Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct)
    {
        // Cheap path first: non-guests bypass entirely. No DB hit at all.
        if (!a.Roles.Contains(SeedData.GuestRole)) return RuleResult.Ok();

        var court = await db.Courts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == a.CourtId, ct);

        // CourtIsActiveRule reports COURT_UNKNOWN if the court row is
        // missing; we don't duplicate that here, just defer.
        if (court is null) return RuleResult.Ok();

        return court.IsGuestBookable
            ? RuleResult.Ok()
            : RuleResult.Fail(
                "GUEST_COURT_NOT_ALLOWED",
                "Dieser Platz ist für Gastbuchungen nicht freigegeben. Bitte wähle einen anderen Platz.");
    }
}
