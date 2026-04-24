using TennisClub.Api.Common.Endpoints;

namespace TennisClub.Api.Features.Members.List;

public sealed class ListMembersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/members", async (
            ListMembersHandler handler,
            CancellationToken ct,
            string? search = null,
            string? status = null,
            string? role = null) =>
        {
            var members = await handler.HandleAsync(search, status, role, ct);
            return Results.Ok(members);
        })
        .RequireAuthorization("Admin");
}
