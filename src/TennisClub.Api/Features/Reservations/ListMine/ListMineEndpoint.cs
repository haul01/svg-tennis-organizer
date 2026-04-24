using System.Security.Claims;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Domain.Enums;

namespace TennisClub.Api.Features.Reservations.ListMine;

public sealed class ListMineEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/reservations/mine", async (
            ListMineHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct,
            bool upcomingOnly = false,
            ReservationStatus? status = null) =>
        {
            var memberId = user.GetMemberId();
            var items = await handler.HandleAsync(memberId, upcomingOnly, status, ct);
            return Results.Ok(items);
        })
        .RequireAuthorization();
}
