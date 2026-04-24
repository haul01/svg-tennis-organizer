using TennisClub.Api.Common.Endpoints;

namespace TennisClub.Api.Features.Courts.List;

public sealed class ListCourtsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/courts", async (
            ListCourtsHandler handler,
            CancellationToken ct,
            bool includeInactive = false) =>
        {
            var courts = await handler.HandleAsync(includeInactive, ct);
            return Results.Ok(courts);
        })
        .RequireAuthorization();
}
