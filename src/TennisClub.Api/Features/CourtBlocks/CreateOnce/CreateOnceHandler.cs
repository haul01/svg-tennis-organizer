using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.CourtBlocks.Shared;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.CourtBlocks.CreateOnce;

public sealed class CreateOnceHandler(
    AppDbContext db,
    BlockConflictChecker conflicts,
    TimeProvider time)
{
    public async Task<Result<CreateOnceResponse>> HandleAsync(
        CreateOnceRequest req, Guid actorId, CancellationToken ct)
    {
        var court = await db.Courts
            .Where(c => c.Id == req.CourtId)
            .Select(c => new { c.Name, c.IsActive })
            .FirstOrDefaultAsync(ct);
        if (court is null) return Result.NotFound("Platz nicht gefunden.");

        var interval = new BlockConflictChecker.BlockInterval(req.CourtId, req.StartsAt, req.EndsAt);
        var overlaps = await conflicts.FindConflictsAsync([interval], ct);

        if (overlaps.Count > 0 && !req.ForceCancelConflicts)
        {
            return Result.Conflict(ConflictMessage(overlaps.Count));
        }

        var now = time.GetUtcNow();
        if (overlaps.Count > 0) BlockConflictChecker.CancelAll(overlaps, now);

        var block = new CourtBlock
        {
            Id = Guid.NewGuid(),
            CourtId = req.CourtId,
            StartsAt = req.StartsAt,
            EndsAt = req.EndsAt,
            Reason = req.Reason.Trim(),
            SeriesId = null,
            CreatedAt = now,
            CreatedByMemberId = actorId
        };
        db.CourtBlocks.Add(block);
        await db.SaveChangesAsync(ct);

        return Result.Success(new CreateOnceResponse(
            new CourtBlockDto(block.Id, block.CourtId, court.Name,
                block.StartsAt, block.EndsAt, block.Reason, null),
            overlaps.Count));
    }

    private static string ConflictMessage(int count) =>
        count == 1
            ? "Eine Buchung überschneidet sich mit diesem Zeitraum."
            : $"{count} Buchungen überschneiden sich mit diesem Zeitraum.";
}

public sealed record CreateOnceResponse(CourtBlockDto Block, int CancelledReservations);
