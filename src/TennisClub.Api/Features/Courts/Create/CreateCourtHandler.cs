using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Courts.List;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Courts.Create;

public sealed class CreateCourtHandler(AppDbContext db)
{
    public async Task<Result<CourtDto>> HandleAsync(
        CreateCourtRequest req, CancellationToken ct)
    {
        // Default DisplayOrder to "one past the current max" so new courts
        // sit at the end of the grid.
        var order = req.DisplayOrder
            ?? ((await db.Courts.MaxAsync(c => (int?)c.DisplayOrder, ct) ?? 0) + 1);

        var court = new Court
        {
            Name = req.Name.Trim(),
            DisplayOrder = order,
            IsActive = true
        };
        db.Courts.Add(court);
        await db.SaveChangesAsync(ct);

        return Result.Success(new CourtDto(court.Id, court.Name, court.DisplayOrder, court.IsActive));
    }
}
