using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Members.Shared;
using TennisClub.Api.Infrastructure.Persistence;
using TennisClub.Api.Infrastructure.Persistence.Seed;

namespace TennisClub.Api.Features.Members.ChangeRole;

/// <summary>
/// Sets the target member to exactly the requested role. Refresh tokens
/// are revoked on success so the user's next /api/auth/refresh call (or
/// re-login) mints a JWT carrying the new role - otherwise the user
/// would keep the old role for up to AccessTokenMinutes.
/// </summary>
public sealed class ChangeRoleHandler(
    UserManager<Member> users,
    AppDbContext db,
    TimeProvider time)
{
    public async Task<Result<MemberDetailDto>> HandleAsync(
        Guid id, ChangeRoleRequest req, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return Result.NotFound("Mitglied nicht gefunden.");

        var currentRoles = await users.GetRolesAsync(user);
        if (currentRoles.Count == 1 && currentRoles[0] == req.Role)
        {
            // No-op: already in the requested role. Return current shape so
            // the client can refresh its cache from the response.
            return Result.Success(Project(user, currentRoles));
        }

        // Last-admin protection: if the target IS an admin and we're moving
        // them out of the admin role, make sure at least one other admin
        // remains. Otherwise the club locks itself out.
        if (currentRoles.Contains(SeedData.AdminRole)
            && req.Role != SeedData.AdminRole)
        {
            var otherAdminCount = await db.UserRoles
                .Where(ur => ur.UserId != user.Id)
                .Join(db.Roles.Where(r => r.Name == SeedData.AdminRole),
                    ur => ur.RoleId, r => r.Id, (_, _) => 1)
                .CountAsync(ct);

            if (otherAdminCount == 0)
            {
                return Result.Invalid(
                    "Mindestens ein Admin muss verbleiben - diese Aktion würde den letzten Admin entfernen.");
            }
        }

        // Replace the entire role set with the single requested role. Other
        // role permutations (Trainer+Member etc.) aren't in scope; admins
        // who need that can do it via the underlying UserManager later.
        if (currentRoles.Count > 0)
        {
            var remove = await users.RemoveFromRolesAsync(user, currentRoles);
            if (!remove.Succeeded) return ToInvalid(remove);
        }
        var add = await users.AddToRoleAsync(user, req.Role);
        if (!add.Succeeded) return ToInvalid(add);

        // Revoke refresh tokens so the new role takes effect on the
        // very next request, not 15 minutes later when the JWT expires.
        var now = time.GetUtcNow();
        await db.RefreshTokens
            .Where(t => t.MemberId == user.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);

        return Result.Success(Project(user, [req.Role]));
    }

    private static Result<MemberDetailDto> ToInvalid(IdentityResult result) =>
        Result.Invalid([..
            result.Errors.Select(e => new ValidationFailure(e.Code, e.Description))
        ]);

    private static MemberDetailDto Project(Member u, IEnumerable<string> roles) =>
        new(u.Id, u.Email ?? "", u.FirstName, u.LastName,
            roles.FirstOrDefault() ?? SeedData.MemberRole, u.IsActive, u.CreatedAt);
}
