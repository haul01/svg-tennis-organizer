using System.Security.Claims;
using FluentValidation;
using Http = Microsoft.AspNetCore.Http;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.CourtBlocks.CreateSeries;

public sealed class CreateSeriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/court-blocks/series", async (
            CreateSeriesRequest req,
            IValidator<CreateSeriesRequest> validator,
            CreateSeriesHandler handler,
            ClaimsPrincipal caller,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(req, caller.GetMemberId(), ct);
            return result.ToHttpResult(onSuccess: r =>
                Http.Results.Created($"/api/court-blocks/series/{r.SeriesId}", r));
        })
        .RequireAuthorization("Admin");
}
