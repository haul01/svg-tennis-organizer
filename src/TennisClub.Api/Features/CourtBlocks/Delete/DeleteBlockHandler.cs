using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.CourtBlocks.Delete;

public sealed class DeleteBlockHandler(AppDbContext db)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken ct)
    {
        var rows = await db.CourtBlocks.Where(b => b.Id == id).ExecuteDeleteAsync(ct);
        return rows == 0 ? Result.NotFound("Platzsperre nicht gefunden.") : Result.Success();
    }

    public async Task<Result<int>> DeleteSeriesAsync(Guid seriesId, CancellationToken ct)
    {
        var rows = await db.CourtBlocks
            .Where(b => b.SeriesId == seriesId)
            .ExecuteDeleteAsync(ct);
        return rows == 0
            ? Result.NotFound("Serie nicht gefunden.")
            : Result.Success(rows);
    }
}
