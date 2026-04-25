using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Features.CourtBlocks.Shared;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.CourtBlocks.List;

public sealed class ListBlocksHandler(AppDbContext db, TimeProvider time)
{
    public async Task<List<CourtBlockDto>> HandleAsync(
        DateTimeOffset? from, DateTimeOffset? to, int? courtId, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        // Default window: from today, unlimited forward. Keeps the admin
        // list focused on upcoming sperren unless a range is requested.
        var fromDate = from ?? new DateTimeOffset(now.Date, TimeSpan.Zero);

        var query = db.CourtBlocks
            .AsNoTracking()
            .Include(b => b.Court)
            .Where(b => b.EndsAt >= fromDate);

        if (to is not null) query = query.Where(b => b.StartsAt <= to);
        if (courtId is not null) query = query.Where(b => b.CourtId == courtId);

        return await query
            .OrderBy(b => b.StartsAt)
            .Select(b => new CourtBlockDto(
                b.Id, b.CourtId, b.Court.Name,
                b.StartsAt, b.EndsAt, b.Reason, b.SeriesId))
            .ToListAsync(ct);
    }
}
