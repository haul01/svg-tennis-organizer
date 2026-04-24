using System.Security.Claims;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Endpoints;

namespace TennisClub.Api.Features.GuestPlayers.ListForMember;

public sealed class ListMyGuestsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/guest-players/mine", async (
            ListMyGuestsHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var guests = await handler.HandleAsync(user.GetMemberId(), ct);
            return Results.Ok(guests);
        })
        .RequireAuthorization();
}
