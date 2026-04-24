using System.Security.Claims;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Endpoints;

namespace TennisClub.Api.Features.Auth.Me;

public sealed class MeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/auth/me", (ClaimsPrincipal user) =>
        {
            return Results.Ok(new
            {
                id = user.GetMemberId(),
                email = user.FindFirstValue("email"),
                firstName = user.FindFirstValue("firstName"),
                lastName = user.FindFirstValue("lastName"),
                roles = user.FindAll("role").Select(c => c.Value).ToArray()
            });
        })
        .RequireAuthorization();
}
