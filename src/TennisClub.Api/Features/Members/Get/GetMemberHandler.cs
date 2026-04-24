using Microsoft.AspNetCore.Identity;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Members.Shared;
using TennisClub.Api.Infrastructure.Persistence.Seed;

namespace TennisClub.Api.Features.Members.Get;

public sealed class GetMemberHandler(UserManager<Member> users)
{
    public async Task<Result<MemberDetailDto>> HandleAsync(Guid id, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return Result.NotFound("Mitglied nicht gefunden.");

        var roles = await users.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? SeedData.MemberRole;

        return Result.Success(new MemberDetailDto(
            user.Id, user.Email ?? "", user.FirstName, user.LastName,
            role, user.IsActive, user.CreatedAt));
    }
}
