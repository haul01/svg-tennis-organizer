using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.CourtBlocks.Shared;

/// <summary>
/// Shared overlap check used by both single-block and series creation.
/// For each block-interval, collects any overlapping ACTIVE reservation.
/// Caller decides whether to fail (no forceCancel) or cancel them.
/// </summary>
public sealed class BlockConflictChecker(AppDbContext db)
{
    public sealed record BlockInterval(int CourtId, DateTimeOffset StartsAt, DateTimeOffset EndsAt);

    public async Task<List<Reservation>> FindConflictsAsync(
        IReadOnlyCollection<BlockInterval> blocks, CancellationToken ct)
    {
        if (blocks.Count == 0) return [];

        // Load candidate reservations from the widest window, then filter per-block.
        // Cheaper than N separate queries for most admin scenarios.
        var minStart = blocks.Min(b => b.StartsAt);
        var maxEnd = blocks.Max(b => b.EndsAt);
        var courtIds = blocks.Select(b => b.CourtId).Distinct().ToList();

        // Member + Court are included so the cancellation notifier can
        // build the mail body without a second roundtrip per reservation.
        var candidates = await db.Reservations
            .Include(r => r.Member)
            .Include(r => r.Court)
            .Where(r => r.Status == ReservationStatus.Active
                && courtIds.Contains(r.CourtId)
                && r.StartsAt < maxEnd
                && r.EndsAt > minStart)
            .ToListAsync(ct);

        return [.. candidates.Where(r => blocks.Any(b =>
            b.CourtId == r.CourtId
            && b.StartsAt < r.EndsAt
            && b.EndsAt > r.StartsAt))];
    }

    public static void CancelAll(
        IEnumerable<Reservation> reservations, DateTimeOffset now)
    {
        foreach (var r in reservations)
        {
            r.Status = ReservationStatus.Cancelled;
            r.CancelledAt = now;
        }
    }
}
