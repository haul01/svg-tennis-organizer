using System.Security.Claims;
using FluentValidation;
using Http = Microsoft.AspNetCore.Http;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.CourtBlocks.CreateOnce;

public sealed class CreateOnceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/court-blocks", async (
            CreateOnceRequest req,
            IValidator<CreateOnceRequest> validator,
            CreateOnceHandler handler,
            ClaimsPrincipal caller,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(req, caller.GetMemberId(), ct);
            return result.ToHttpResult(onSuccess: r =>
                Http.Results.Created($"/api/court-blocks/{r.Block.Id}", r));
        })
        .RequireAuthorization("Admin");
}
