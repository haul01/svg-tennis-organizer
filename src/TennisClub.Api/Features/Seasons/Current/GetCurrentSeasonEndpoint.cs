using TennisClub.Api.Common.Endpoints;

namespace TennisClub.Api.Features.Seasons.Current;

public sealed class GetCurrentSeasonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/seasons/current", async (
            GetCurrentSeasonHandler handler,
            CancellationToken ct) =>
        {
            var season = await handler.HandleAsync(ct);
            return season is null
                ? Results.NoContent()
                : Results.Ok(season);
        })
        .RequireAuthorization();
}
