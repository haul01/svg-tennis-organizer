using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.ListForWeek;

public sealed class ListForWeekHandler(AppDbContext db)
{
    public async Task<IReadOnlyList<WeekReservationDto>> HandleAsync(
        DateTimeOffset weekStart, Guid memberId, CancellationToken ct)
    {
        // Normalize to a full 7-day window starting at 00:00 on weekStart's date.
        var from = new DateTimeOffset(weekStart.Year, weekStart.Month, weekStart.Day,
            0, 0, 0, weekStart.Offset);
        var to = from.AddDays(7);

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
