using TennisClub.Api.Common.Endpoints;

namespace TennisClub.Api.Features.Settings.Public;

public sealed class GetPublicSettingsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/settings/public", async (
            GetPublicSettingsHandler handler,
            CancellationToken ct) =>
        {
            var settings = await handler.HandleAsync(ct);
            return Results.Ok(settings);
        })
        .RequireAuthorization();
}
