using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Reservations.Cancel;

public sealed class CancelReservationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/reservations/{id:guid}", async (
            Guid id,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            CancelReservationHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(ifMatch))
            {
                return Results.BadRequest(new
                {
                    error = "If-Match-Header mit RowVersion wird benötigt."
                });
            }

            byte[] rowVersion;
            try
            {
                // ETag-Values sind üblicherweise gequotet ("abc=="); optional tolerieren.
                rowVersion = Convert.FromBase64String(ifMatch.Trim('"'));
            }
            catch (FormatException)
            {
                return Results.BadRequest(new
                {
                    error = "If-Match-Header ist kein gültiger Base64-Wert."
                });
            }

            var memberId = user.GetMemberId();
            var result = await handler.HandleAsync(id, memberId, rowVersion, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization();
}
