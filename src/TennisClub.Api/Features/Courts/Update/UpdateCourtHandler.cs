using TennisClub.Api.Common.Results;
using TennisClub.Api.Features.Courts.List;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Courts.Update;

public sealed class UpdateCourtHandler(AppDbContext db)
{
    public async Task<Result<CourtDto>> HandleAsync(
        int id, UpdateCourtRequest req, CancellationToken ct)
    {
        var court = await db.Courts.FindAsync([id], ct);
        if (court is null) return Result.NotFound("Platz nicht gefunden.");

        court.Name = req.Name.Trim();
        court.DisplayOrder = req.DisplayOrder;
        court.IsActive = req.IsActive;
        court.IsGuestBookable = req.IsGuestBookable;

        await db.SaveChangesAsync(ct);

        return Result.Success(new CourtDto(
            court.Id, court.Name, court.DisplayOrder, court.IsActive, court.IsGuestBookable));
    }
}
