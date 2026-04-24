using Microsoft.AspNetCore.Identity;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Members.Shared;

namespace TennisClub.Api.Features.Members.Update;

public sealed class UpdateMemberHandler(UserManager<Member> users)
{
    public async Task<Result<MemberDetailDto>> HandleAsync(
        Guid id, UpdateMemberRequest req, Guid actorId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return Result.NotFound("Mitglied nicht gefunden.");

        user.FirstName = req.FirstName.Trim();
        user.LastName = req.LastName.Trim();

        var newEmail = req.Email.Trim();
        if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            var setEmail = await users.SetEmailAsync(user, newEmail);
            if (!setEmail.Succeeded) return MapErrors(setEmail);

            var setUser = await users.SetUserNameAsync(user, newEmail);
            if (!setUser.Succeeded) return MapErrors(setUser);
        }

        var update = await users.UpdateAsync(user);
        if (!update.Succeeded) return MapErrors(update);

        var roles = await users.GetRolesAsync(user);
        var currentRole = roles.FirstOrDefault();
        if (currentRole != req.Role)
        {
            // Guard: admins can't demote themselves - easy lock-out foot-gun.
            if (user.Id == actorId && req.Role != "Admin")
            {
                return Result.Invalid("Administratoren können ihre eigene Admin-Rolle nicht entfernen.");
            }

            if (roles.Count > 0)
            {
                var remove = await users.RemoveFromRolesAsync(user, roles);
                if (!remove.Succeeded) return MapErrors(remove);
            }
            var add = await users.AddToRoleAsync(user, req.Role);
            if (!add.Succeeded) return MapErrors(add);
        }

        return Result.Success(new MemberDetailDto(
            user.Id, user.Email ?? "", user.FirstName, user.LastName,
            req.Role, user.IsActive, user.CreatedAt));
    }

    private static Result MapErrors(IdentityResult result) =>
        Result.Invalid([.. result.Errors
            .Select(e => new ValidationFailure(e.Code, e.Description))]);
}
