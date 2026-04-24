using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Auth.Shared;
using TennisClub.Api.Infrastructure.Auth;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Auth.Refresh;

public sealed class RefreshHandler(
    AppDbContext db,
    UserManager<Member> users,
    JwtTokenService jwt,
    JwtSettings settings,
    TimeProvider time)
{
    public async Task<Result<AuthResponse>> HandleAsync(
        RefreshRequest req, CancellationToken ct)
    {
        var hash = JwtTokenService.Hash(req.RefreshToken);
        var now = time.GetUtcNow();

        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null || token.RevokedAt is not null || token.ExpiresAt <= now)
            return Result.Unauthorized();

        var user = await users.FindByIdAsync(token.MemberId.ToString());
        if (user is null || !user.IsActive)
            return Result.Unauthorized();

        // Revoke the old token immediately (prevents replay).
        token.RevokedAt = now;

        var roles = await users.GetRolesAsync(user);
        var newAccess = jwt.CreateAccessToken(user, roles);
        var newRefresh = jwt.CreateRefreshToken();

        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            MemberId = user.Id,
            TokenHash = JwtTokenService.Hash(newRefresh),
            ExpiresAt = now.AddDays(settings.RefreshTokenDays),
            CreatedAt = now
        };
        token.ReplacedByTokenId = replacement.Id;
        db.RefreshTokens.Add(replacement);
        await db.SaveChangesAsync(ct);

        return Result.Success(new AuthResponse(newAccess, newRefresh));
    }
}
