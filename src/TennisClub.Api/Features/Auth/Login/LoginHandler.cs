using Microsoft.AspNetCore.Identity;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Auth.Shared;
using TennisClub.Api.Infrastructure.Auth;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Auth.Login;

public sealed class LoginHandler(
    UserManager<Member> users,
    AppDbContext db,
    JwtTokenService jwt,
    JwtSettings settings,
    TimeProvider time)
{
    public async Task<Result<AuthResponse>> HandleAsync(
        LoginRequest req, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(req.Email);

        // Intentionally generic everywhere below - prevents account enumeration
        // (unknown email, wrong password, inactive and locked-out all look alike).
        if (user is null || !user.IsActive)
        {
            return Result.Unauthorized("Login fehlgeschlagen.");
        }

        // Drive the Identity lockout state machine that Program.cs configures
        // (5 failures -> 15 min lockout). CheckPasswordAsync alone never
        // touches it, so without this the brute-force protection is inert.
        if (await users.IsLockedOutAsync(user))
        {
            return Result.Unauthorized("Login fehlgeschlagen.");
        }

        if (!await users.CheckPasswordAsync(user, req.Password))
        {
            await users.AccessFailedAsync(user);
            return Result.Unauthorized("Login fehlgeschlagen.");
        }

        await users.ResetAccessFailedCountAsync(user);

        var roles = await users.GetRolesAsync(user);
        var accessToken = jwt.CreateAccessToken(user, roles);
        var refreshToken = jwt.CreateRefreshToken();
        var now = time.GetUtcNow();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            MemberId = user.Id,
            TokenHash = JwtTokenService.Hash(refreshToken),
            ExpiresAt = now.AddDays(settings.RefreshTokenDays),
            CreatedAt = now
        });
        await db.SaveChangesAsync(ct);

        return Result.Success(new AuthResponse(accessToken, refreshToken));
    }
}
