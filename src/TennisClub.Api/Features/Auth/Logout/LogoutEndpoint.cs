using System.Security.Claims;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Endpoints;

namespace TennisClub.Api.Features.Auth.Logout;

public sealed class LogoutEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/auth/logout", async (
            LogoutHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            await handler.HandleAsync(user.GetMemberId(), ct);
            return Results.NoContent();
        })
        .RequireAuthorization();
}
