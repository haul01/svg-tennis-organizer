using FluentValidation;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Seasons.Update;

public sealed class UpdateSeasonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("/api/seasons/{id:int}", async (
            int id,
            UpdateSeasonRequest req,
            IValidator<UpdateSeasonRequest> validator,
            UpdateSeasonHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(id, req, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization("Admin");
}
