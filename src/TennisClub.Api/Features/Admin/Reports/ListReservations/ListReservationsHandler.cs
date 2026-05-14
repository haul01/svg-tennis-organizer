using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Admin.Reports.ListReservations;

/// <summary>
/// Paginated reservation listing for admins. Returns one row per
/// reservation (active and cancelled), sorted by StartsAt desc, with
/// member + court + guest names already joined so the UI can render
/// without N+1 lookups.
/// </summary>
public sealed class ListReservationsHandler(AppDbContext db, TimeProvider time)
{
    private const int MaxPageSize = 100;
    private const int DefaultRangeDays = 30;

    public async Task<ListReservationsResponse> HandleAsync(
        ListReservationsRequest req, CancellationToken ct)
    {
        var page = Math.Max(1, req.Page);
        var size = Math.Clamp(req.PageSize, 1, MaxPageSize);

        // Default window: 30 days back, 30 days forward. Picks up the
        // typical "what happened lately + what's coming up next" view.
        var now = time.GetUtcNow();
        var from = req.From ?? now.AddDays(-DefaultRangeDays);
        var to = req.To ?? now.AddDays(DefaultRangeDays);

        var query = db.Reservations
            .AsNoTracking()
            .Where(r => r.StartsAt >= from && r.StartsAt < to);

        if (req.CourtId is int courtId)
            query = query.Where(r => r.CourtId == courtId);

        if (req.Status is { } status)
            query = query.Where(r => r.Status == status);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.StartsAt)
            .ThenBy(r => r.CourtId)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(r => new ReservationReportItemDto(
                r.Id,
                r.StartsAt,
                r.EndsAt,
                r.Court.Name,
                r.Member.FirstName,
                r.Member.LastName,
                r.Member.Email!,
                r.HasGuest,
                r.GuestPlayer != null
                    ? (r.GuestPlayer.FirstName + " " + r.GuestPlayer.LastName)
                    : null,
                r.Status,
                r.CreatedAt,
                r.CancelledAt))
            .ToListAsync(ct);

        return new ListReservationsResponse(items, totalCount, page, size);
    }
}
