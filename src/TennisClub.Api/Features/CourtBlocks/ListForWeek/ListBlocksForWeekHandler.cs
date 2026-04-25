using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Features.CourtBlocks.Shared;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.CourtBlocks.ListForWeek;

public sealed class ListBlocksForWeekHandler(AppDbContext db)
{
    public async Task<List<CourtBlockDto>> HandleAsync(
        DateTimeOffset weekStart, CancellationToken ct)
    {
        // weekStart already represents local Monday 00:00 (serialized as UTC
        // by the client). Rebuilding it from .Year/.Month/.Day loses the
        // offset and shifts the window by one day for clients east of UTC.
        var from = weekStart;
        var to = weekStart.AddDays(7);

        return await db.CourtBlocks
            .AsNoTracking()
            .Include(b => b.Court)
            .Where(b => b.StartsAt < to && b.EndsAt > from)
            .OrderBy(b => b.StartsAt)
            .Select(b => new CourtBlockDto(
                b.Id, b.CourtId, b.Court.Name,
                b.StartsAt, b.EndsAt, b.Reason, b.SeriesId))
            .ToListAsync(ct);
    }
}
