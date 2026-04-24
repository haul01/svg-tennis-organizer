using System.Security.Claims;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Members.SetActive;

public sealed class SetActiveEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/members/{id:guid}/set-active", async (
            Guid id,
            SetActiveRequest req,
            SetActiveHandler handler,
            ClaimsPrincipal caller,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, req, caller.GetMemberId(), ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization("Admin");
}
