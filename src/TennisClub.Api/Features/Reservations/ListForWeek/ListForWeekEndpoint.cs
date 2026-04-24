using System.Security.Claims;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Endpoints;

namespace TennisClub.Api.Features.Reservations.ListForWeek;

public sealed class ListForWeekEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/reservations/week", async (
            DateTimeOffset startDate,
            ListForWeekHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.GetMemberId();
            var reservations = await handler.HandleAsync(startDate, memberId, ct);
            return Results.Ok(reservations);
        })
        .RequireAuthorization();
}
