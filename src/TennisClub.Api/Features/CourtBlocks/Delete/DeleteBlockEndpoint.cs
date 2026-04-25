using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.CourtBlocks.Delete;

public sealed class DeleteBlockEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/court-blocks/{id:guid}", async (
            Guid id,
            DeleteBlockHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization("Admin");

        app.MapDelete("/api/court-blocks/series/{seriesId:guid}", async (
            Guid seriesId,
            DeleteBlockHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.DeleteSeriesAsync(seriesId, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization("Admin");
    }
}
