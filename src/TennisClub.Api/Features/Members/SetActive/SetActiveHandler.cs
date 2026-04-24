using Microsoft.AspNetCore.Identity;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Members.Shared;

namespace TennisClub.Api.Features.Members.SetActive;

public sealed class SetActiveHandler(UserManager<Member> users)
{
    public async Task<Result<MemberDetailDto>> HandleAsync(
        Guid id, SetActiveRequest req, Guid actorId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return Result.NotFound("Mitglied nicht gefunden.");

        if (user.Id == actorId && !req.IsActive)
        {
            return Result.Invalid("Du kannst dich nicht selbst deaktivieren.");
        }

        if (user.IsActive == req.IsActive)
        {
            // No-op: report current state so the client can update its cache.
            var currentRoles = await users.GetRolesAsync(user);
            return Result.Success(Project(user, currentRoles));
        }

        user.IsActive = req.IsActive;
        var update = await users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return Result.Invalid([.. update.Errors
                .Select(e => new ValidationFailure(e.Code, e.Description))]);
        }

        var roles = await users.GetRolesAsync(user);
        return Result.Success(Project(user, roles));
    }

    private static MemberDetailDto Project(Member user, IList<string> roles) =>
        new(user.Id, user.Email ?? "", user.FirstName, user.LastName,
            roles.FirstOrDefault() ?? "Member", user.IsActive, user.CreatedAt);
}
