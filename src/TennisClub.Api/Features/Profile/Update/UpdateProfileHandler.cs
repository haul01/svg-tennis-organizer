using Microsoft.AspNetCore.Identity;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Profile.Shared;

namespace TennisClub.Api.Features.Profile.Update;

public sealed class UpdateProfileHandler(UserManager<Member> users)
{
    public async Task<Result<ProfileDto>> HandleAsync(
        Guid memberId, UpdateProfileRequest req, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(memberId.ToString());
        if (user is null) return Result.NotFound("Mitglied nicht gefunden.");

        user.FirstName = req.FirstName.Trim();
        user.LastName = req.LastName.Trim();

        var update = await users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            var failures = update.Errors
                .Select(e => new ValidationFailure(e.Code, e.Description))
                .ToList();
            return Result.Invalid(failures);
        }

        var roles = await users.GetRolesAsync(user);
        return Result.Success(new ProfileDto(
            user.Id, user.Email!, user.FirstName, user.LastName, [.. roles]));
    }
}
