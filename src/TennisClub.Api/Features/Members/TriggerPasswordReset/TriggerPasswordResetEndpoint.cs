using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Members.TriggerPasswordReset;

public sealed class TriggerPasswordResetEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/members/{id:guid}/reset-password", async (
            Guid id,
            TriggerPasswordResetHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization("Admin");
}
