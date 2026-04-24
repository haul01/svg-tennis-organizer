using FluentValidation;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Auth.Login;

public sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/auth/login", async (
            LoginRequest req,
            IValidator<LoginRequest> validator,
            LoginHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(req, ct);
            return result.ToHttpResult();
        })
        .RequireRateLimiting("auth-login")
        .AllowAnonymous();
}
