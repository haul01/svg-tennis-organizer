using System.Security.Claims;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Reservations.Cancel;

public sealed class CancelReservationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/reservations/{id:guid}", async (
            Guid id,
            CancelReservationHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.GetMemberId();
            var result = await handler.HandleAsync(id, memberId, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization();
}
