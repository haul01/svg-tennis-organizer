using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.ListMine;

public sealed class ListMineHandler(AppDbContext db, TimeProvider time)
{
    /// <param name="upcomingOnly">When true, only reservations that start in
    /// the future are returned. Default lets the UI split into "coming" /
    /// "past" tabs on its own.</param>
    /// <param name="statusFilter">When null, all statuses are returned.</param>
    public async Task<IReadOnlyList<MyReservationDto>> HandleAsync(
        Guid memberId,
        bool upcomingOnly,
        ReservationStatus? statusFilter,
        CancellationToken ct)
    {
        var now = time.GetUtcNow();

        var query = db.Reservations
            .AsNoTracking()
            .Where(r => r.MemberId == memberId);

        if (statusFilter is { } s)
            query = query.Where(r => r.Status == s);

        if (upcomingOnly)
            query = query.Where(r => r.StartsAt > now);

        return await query
            .OrderBy(r => r.StartsAt)
            .Select(r => new MyReservationDto(
                r.Id,
                r.CourtId,
                r.Court.Name,
                r.StartsAt,
                r.EndsAt,
                r.Status,
                r.CancelledAt,
                r.GuestPlayer != null
                    ? (r.GuestPlayer.FirstName + " " + r.GuestPlayer.LastName)
                    : null,
                r.RowVersion))
            .ToListAsync(ct);
    }
}
