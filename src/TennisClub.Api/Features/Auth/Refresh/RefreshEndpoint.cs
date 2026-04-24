using FluentValidation;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Auth.Refresh;

public sealed class RefreshEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/auth/refresh", async (
            RefreshRequest req,
            IValidator<RefreshRequest> validator,
            RefreshHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(req, ct);
            return result.ToHttpResult();
        })
        .AllowAnonymous();
}
