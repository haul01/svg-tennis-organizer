using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.CourtBlocks.Shared;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.CourtBlocks.CreateSeries;

public sealed class CreateSeriesHandler(
    AppDbContext db,
    BlockConflictChecker conflicts,
    TimeProvider time)
{
    public async Task<Result<CreateSeriesResponse>> HandleAsync(
        CreateSeriesRequest req, Guid actorId, CancellationToken ct)
    {
        var court = await db.Courts
            .Where(c => c.Id == req.CourtId)
            .Select(c => new { c.Name })
            .FirstOrDefaultAsync(ct);
        if (court is null) return Result.NotFound("Platz nicht gefunden.");

        var intervals = ExpandSeries(req);
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

        var seriesId = Guid.NewGuid();
        var blocks = intervals.Select(i => new CourtBlock
        {
            Id = Guid.NewGuid(),
            CourtId = i.CourtId,
            StartsAt = i.StartsAt,
            EndsAt = i.EndsAt,
            Reason = req.Reason.Trim(),
            SeriesId = seriesId,
            CreatedAt = now,
            CreatedByMemberId = actorId
        }).ToList();

        db.CourtBlocks.AddRange(blocks);
        await db.SaveChangesAsync(ct);

        return Result.Success(new CreateSeriesResponse(seriesId, blocks.Count, overlaps.Count));
    }

    private static List<BlockConflictChecker.BlockInterval> ExpandSeries(CreateSeriesRequest req)
    {
        var list = new List<BlockConflictChecker.BlockInterval>();

        // Step to the first matching weekday on or after StartDate.
        var cursor = req.StartDate;
        var dayShift = ((int)req.Weekday - (int)cursor.DayOfWeek + 7) % 7;
        cursor = cursor.AddDays(dayShift);

        while (cursor <= req.EndDate)
        {
            // Keep the wall-clock offset from the request (Austrian local time)
            // so the interval stays within the intended slot even across DST.
            var start = new DateTimeOffset(
                cursor.ToDateTime(req.StartTime), TimeSpan.Zero);
            var end = new DateTimeOffset(
                cursor.ToDateTime(req.EndTime), TimeSpan.Zero);
            list.Add(new BlockConflictChecker.BlockInterval(req.CourtId, start, end));

            cursor = cursor.AddDays(7);
        }

        return list;
    }
}

public sealed record CreateSeriesResponse(Guid SeriesId, int BlocksCreated, int CancelledReservations);
