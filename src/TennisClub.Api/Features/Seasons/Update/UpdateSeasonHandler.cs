using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Features.Seasons.Current;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Seasons.Update;

public sealed class UpdateSeasonHandler(AppDbContext db)
{
    public async Task<Result<SeasonDto>> HandleAsync(
        int id, UpdateSeasonRequest req, CancellationToken ct)
    {
        var season = await db.Seasons.FindAsync([id], ct);
        if (season is null) return Result.NotFound("Saison nicht gefunden.");

        season.Name = req.Name.Trim();
        season.StartDate = req.StartDate;
        season.EndDate = req.EndDate;
        season.OpeningTime = req.OpeningTime;
        season.ClosingTime = req.ClosingTime;
        season.SlotDurationMinutes = req.SlotDurationMinutes;

        await db.SaveChangesAsync(ct);

        return Result.Success(new SeasonDto(
            season.Id, season.Name,
            season.StartDate, season.EndDate,
            season.OpeningTime, season.ClosingTime,
            season.SlotDurationMinutes));
    }
}
