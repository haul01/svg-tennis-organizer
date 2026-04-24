using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Auth.Logout;

public sealed class LogoutHandler(AppDbContext db, TimeProvider time)
{
    public async Task HandleAsync(Guid memberId, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        await db.RefreshTokens
            .Where(t => t.MemberId == memberId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }
}
