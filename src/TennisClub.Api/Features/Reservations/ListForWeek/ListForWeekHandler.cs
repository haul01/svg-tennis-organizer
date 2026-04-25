using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.ListForWeek;

public sealed class ListForWeekHandler(AppDbContext db)
{
    public async Task<IReadOnlyList<WeekReservationDto>> HandleAsync(
        DateTimeOffset weekStart, Guid memberId, CancellationToken ct)
    {
        // weekStart already represents local Monday 00:00 (serialized as UTC
        // by the client). Rebuilding it from .Year/.Month/.Day loses the
        // offset and shifts the window by one day for clients east of UTC.
        var from = weekStart;
        var to = weekStart.AddDays(7);

        var rows = await db.Reservations
            .AsNoTracking()
            .Where(r => r.Status == ReservationStatus.Active
                && r.StartsAt >= from
                && r.StartsAt < to)
            .Select(r => new
            {
                r.Id,
                r.CourtId,
                r.StartsAt,
                r.EndsAt,
                r.MemberId,
                GuestFirst = r.GuestPlayer != null ? r.GuestPlayer.FirstName : null,
                GuestLast = r.GuestPlayer != null ? r.GuestPlayer.LastName : null
            })
            .ToListAsync(ct);

        return [.. rows.Select(r =>
        {
            var isMine = r.MemberId == memberId;
            var guestName = isMine && r.GuestFirst is not null
                ? $"{r.GuestFirst} {r.GuestLast}".Trim()
                : null;
            return new WeekReservationDto(r.Id, r.CourtId, r.StartsAt, r.EndsAt, isMine, guestName);
        })];
    }
}
