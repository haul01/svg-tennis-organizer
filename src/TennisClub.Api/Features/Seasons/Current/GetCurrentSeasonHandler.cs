using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Seasons.Current;

public sealed class GetCurrentSeasonHandler(AppDbContext db, TimeProvider time)
{
    public async Task<SeasonDto?> HandleAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var season = await db.Seasons
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StartDate <= today && s.EndDate >= today, ct);

        if (season is null) return null;
        return new SeasonDto(
            season.Id,
            season.Name,
            season.StartDate,
            season.EndDate,
            season.OpeningTime,
            season.ClosingTime,
            season.SlotDurationMinutes);
    }
}
