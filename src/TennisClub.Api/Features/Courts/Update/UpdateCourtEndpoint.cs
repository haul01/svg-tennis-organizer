using FluentValidation;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Courts.Update;

public sealed class UpdateCourtEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("/api/courts/{id:int}", async (
            int id,
            UpdateCourtRequest req,
            IValidator<UpdateCourtRequest> validator,
            UpdateCourtHandler handler,
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
