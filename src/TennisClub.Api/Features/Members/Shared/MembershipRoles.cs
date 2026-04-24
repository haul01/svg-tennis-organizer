using TennisClub.Api.Infrastructure.Persistence.Seed;

namespace TennisClub.Api.Features.Members.Shared;

public static class MembershipRoles
{
    public static readonly string[] All =
        [SeedData.MemberRole, SeedData.TrainerRole, SeedData.AdminRole];

    public static bool IsKnown(string role) => All.Contains(role);
}
