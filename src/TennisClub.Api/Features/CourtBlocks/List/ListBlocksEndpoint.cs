using TennisClub.Api.Common.Endpoints;

namespace TennisClub.Api.Features.CourtBlocks.List;

public sealed class ListBlocksEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/court-blocks", async (
            ListBlocksHandler handler,
            CancellationToken ct,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            int? courtId = null) =>
        {
            var blocks = await handler.HandleAsync(from, to, courtId, ct);
            return Results.Ok(blocks);
        })
        .RequireAuthorization("Admin");
}
