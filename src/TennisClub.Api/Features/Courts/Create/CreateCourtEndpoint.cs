using FluentValidation;
using Http = Microsoft.AspNetCore.Http;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Courts.Create;

public sealed class CreateCourtEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/courts", async (
            CreateCourtRequest req,
            IValidator<CreateCourtRequest> validator,
            CreateCourtHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(req, ct);
            return result.ToHttpResult(
                onSuccess: c => Http.Results.Created($"/api/courts/{c.Id}", c));
        })
        .RequireAuthorization("Admin");
}
