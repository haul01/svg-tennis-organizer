using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Features.GuestPlayers.Shared;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.GuestPlayers.ListForMember;

public sealed class ListMyGuestsHandler(AppDbContext db)
{
    public Task<List<GuestPlayerDto>> HandleAsync(Guid memberId, CancellationToken ct) =>
        db.GuestPlayers
            .AsNoTracking()
            .Where(g => g.InvitedByMemberId == memberId && g.IsActive)
            .OrderBy(g => g.LastName).ThenBy(g => g.FirstName)
            .Select(g => new GuestPlayerDto(
                g.Id, g.FirstName, g.LastName, g.Email, g.IsActive, g.CreatedAt))
            .ToListAsync(ct);
}
