using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TennisClub.Api.Common.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetMemberId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Member id (sub) claim missing.");

        return Guid.Parse(sub);
    }

    public static bool TryGetMemberId(this ClaimsPrincipal user, out Guid memberId)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(sub, out memberId);
    }
}
