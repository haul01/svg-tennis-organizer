using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Courts.List;

public sealed class ListCourtsHandler(AppDbContext db)
{
    public Task<List<CourtDto>> HandleAsync(bool includeInactive, CancellationToken ct)
    {
        var query = db.Courts.AsNoTracking();
        if (!includeInactive) query = query.Where(c => c.IsActive);

        return query
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CourtDto(c.Id, c.Name, c.DisplayOrder, c.IsActive))
            .ToListAsync(ct);
    }
}
