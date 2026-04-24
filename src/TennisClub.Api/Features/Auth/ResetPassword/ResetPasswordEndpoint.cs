using FluentValidation;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Auth.ResetPassword;

public sealed class ResetPasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/auth/reset-password", async (
            ResetPasswordRequest req,
            IValidator<ResetPasswordRequest> validator,
            ResetPasswordHandler handler,
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
