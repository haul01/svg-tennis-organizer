using Microsoft.AspNetCore.Identity;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Profile.Shared;

namespace TennisClub.Api.Features.Profile.Get;

public sealed class GetProfileHandler(UserManager<Member> users)
{
    public async Task<Result<ProfileDto>> HandleAsync(Guid memberId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(memberId.ToString());
        if (user is null) return Result.NotFound("Mitglied nicht gefunden.");

        var roles = await users.GetRolesAsync(user);
        return Result.Success(new ProfileDto(
            user.Id, user.Email!, user.FirstName, user.LastName, [.. roles]));
    }
}
