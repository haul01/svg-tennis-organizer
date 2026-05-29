using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Common.Time;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.CourtBlocks.Shared;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.CourtBlocks.CreateSeries;

public sealed class CreateSeriesHandler(
    AppDbContext db,
    BlockConflictChecker conflicts,
    BlockCancellationNotifier notifier,
    TimeProvider time)
{
    public async Task<Result<CreateSeriesResponse>> HandleAsync(
        CreateSeriesRequest req, Guid actorId, CancellationToken ct)
    {
        var targetCourts = await ResolveCourtsAsync(req, ct);
        if (targetCourts is null) return Result.NotFound("Platz nicht gefunden.");
        if (targetCourts.Count == 0)
        {
            return Result.Invalid(
                "Keine aktiven Plätze vorhanden - Sperre kann nicht angelegt werden.");
        }

        // Expand (weeks x courts). All-courts mode multiplies the basic
        // weekly expansion across every active court.
        var intervals = ExpandSeries(req, targetCourts);
        if (intervals.Count == 0)
        {
            return Result.Invalid("Der gewählte Zeitraum enthält keinen passenden Wochentag.");
        }

        var overlaps = await conflicts.FindConflictsAsync(intervals, ct);

        if (overlaps.Count > 0 && !req.ForceCancelConflicts)
        {
            return Result.Conflict(
                overlaps.Count == 1
                    ? "Eine Buchung überschneidet sich mit der Serie."
                    : $"{overlaps.Count} Buchungen überschneiden sich mit der Serie.");
        }

        var now = time.GetUtcNow();
        if (overlaps.Count > 0) BlockConflictChecker.CancelAll(overlaps, now);

        var reason = req.Reason.Trim();
        var seriesId = Guid.NewGuid();
        var blocks = intervals.Select(i => new CourtBlock
        {
            Id = Guid.NewGuid(),
            CourtId = i.CourtId,
            StartsAt = i.StartsAt,
            EndsAt = i.EndsAt,
            Reason = reason,
            SeriesId = seriesId,
            CreatedAt = now,
            CreatedByMemberId = actorId
        }).ToList();

        db.CourtBlocks.AddRange(blocks);
        await db.SaveChangesAsync(ct);

        // Best-effort: notify after persistence so mail failures don't
        // roll back the (already saved) cancellations.
        if (overlaps.Count > 0)
        {
            await notifier.NotifyCancelledAsync(overlaps, reason, ct);
        }

        return Result.Success(new CreateSeriesResponse(seriesId, blocks.Count, overlaps.Count));
    }

    private async Task<IReadOnlyList<int>?> ResolveCourtsAsync(
        CreateSeriesRequest req, CancellationToken ct)
    {
        if (req.AllCourts)
        {
            return await db.Courts
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => c.Id)
                .ToListAsync(ct);
        }

        var exists = await db.Courts
            .AsNoTracking()
            .AnyAsync(c => c.Id == req.CourtId, ct);
        return exists ? [req.CourtId] : null;
    }

    internal static List<BlockConflictChecker.BlockInterval> ExpandSeries(
        CreateSeriesRequest req, IReadOnlyList<int> courtIds)
    {
        var list = new List<BlockConflictChecker.BlockInterval>();

        // Step to the first matching weekday on or after StartDate.
        var cursor = req.StartDate;
        var dayShift = ((int)req.Weekday - (int)cursor.DayOfWeek + 7) % 7;
        cursor = cursor.AddDays(dayShift);

        while (cursor <= req.EndDate)
        {
            // StartTime/EndTime are Austrian wall-clock times. Convert each
            // occurrence to a real instant via the club timezone (DST-aware)
            // so the block lands on the same local slot members book — using
            // TimeSpan.Zero here would store the time as UTC and shift the
            // block 1-2h off the intended hour.
            var start = ClubTimeZone.ToInstant(cursor.ToDateTime(req.StartTime));
            var end = ClubTimeZone.ToInstant(cursor.ToDateTime(req.EndTime));
            foreach (var cid in courtIds)
            {
                list.Add(new BlockConflictChecker.BlockInterval(cid, start, end));
            }

            cursor = cursor.AddDays(7);
        }

        return list;
    }
}

public sealed record CreateSeriesResponse(Guid SeriesId, int BlocksCreated, int CancelledReservations);
