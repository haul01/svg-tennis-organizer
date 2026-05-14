using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.CourtBlocks.Shared;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.CourtBlocks.CreateOnce;

public sealed class CreateOnceHandler(
    AppDbContext db,
    BlockConflictChecker conflicts,
    BlockCancellationNotifier notifier,
    TimeProvider time)
{
    public async Task<Result<CreateOnceResponse>> HandleAsync(
        CreateOnceRequest req, Guid actorId, CancellationToken ct)
    {
        // Resolve target courts up front so single-court and all-courts
        // mode share the same downstream pipeline.
        var courts = await ResolveCourtsAsync(req, ct);
        if (courts is null) return Result.NotFound("Platz nicht gefunden.");
        if (courts.Count == 0)
        {
            return Result.Invalid(
                "Keine aktiven Plätze vorhanden - Sperre kann nicht angelegt werden.");
        }

        var intervals = courts
            .Select(c => new BlockConflictChecker.BlockInterval(c.Id, req.StartsAt, req.EndsAt))
            .ToList();
        var overlaps = await conflicts.FindConflictsAsync(intervals, ct);

        if (overlaps.Count > 0 && !req.ForceCancelConflicts)
        {
            return Result.Conflict(ConflictMessage(overlaps.Count));
        }

        var now = time.GetUtcNow();
        if (overlaps.Count > 0) BlockConflictChecker.CancelAll(overlaps, now);

        var reason = req.Reason.Trim();
        // AllCourts mode produces one CourtBlock row per court but they
        // share one SeriesId so the admin can delete them as a unit.
        // Single-court mode keeps SeriesId null (no group).
        var seriesId = req.AllCourts ? Guid.NewGuid() : (Guid?)null;
        var blocks = courts.Select(c => new CourtBlock
        {
            Id = Guid.NewGuid(),
            CourtId = c.Id,
            StartsAt = req.StartsAt,
            EndsAt = req.EndsAt,
            Reason = reason,
            SeriesId = seriesId,
            CreatedAt = now,
            CreatedByMemberId = actorId
        }).ToList();
        db.CourtBlocks.AddRange(blocks);
        await db.SaveChangesAsync(ct);

        // Best-effort: notify after the block is persisted. Mail failures
        // are logged inside the notifier, never roll back the cancellations.
        if (overlaps.Count > 0)
        {
            await notifier.NotifyCancelledAsync(overlaps, reason, ct);
        }

        // The response carries one representative block for backward
        // compatibility (admin UI only reads the cancellation count and
        // then closes). Court name comes from the first picked court.
        var first = blocks[0];
        var firstCourt = courts.First(c => c.Id == first.CourtId);
        return Result.Success(new CreateOnceResponse(
            new CourtBlockDto(first.Id, first.CourtId, firstCourt.Name,
                first.StartsAt, first.EndsAt, first.Reason, first.SeriesId),
            overlaps.Count));
    }

    private async Task<IReadOnlyList<CourtRef>?> ResolveCourtsAsync(
        CreateOnceRequest req, CancellationToken ct)
    {
        if (req.AllCourts)
        {
            return await db.Courts
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CourtRef(c.Id, c.Name))
                .ToListAsync(ct);
        }

        var court = await db.Courts
            .AsNoTracking()
            .Where(c => c.Id == req.CourtId)
            .Select(c => new CourtRef(c.Id, c.Name))
            .FirstOrDefaultAsync(ct);
        return court is null ? null : [court];
    }

    private sealed record CourtRef(int Id, string Name);

    private static string ConflictMessage(int count) =>
        count == 1
            ? "Eine Buchung überschneidet sich mit diesem Zeitraum."
            : $"{count} Buchungen überschneiden sich mit diesem Zeitraum.";
}

public sealed record CreateOnceResponse(CourtBlockDto Block, int CancelledReservations);
