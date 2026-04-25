using TennisClub.Api.Common.Endpoints;

namespace TennisClub.Api.Features.CourtBlocks.ListForWeek;

public sealed class ListBlocksForWeekEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/court-blocks/week", async (
            DateTimeOffset startDate,
            ListBlocksForWeekHandler handler,
            CancellationToken ct) =>
        {
            var blocks = await handler.HandleAsync(startDate, ct);
            return Results.Ok(blocks);
        })
        .RequireAuthorization();
}
