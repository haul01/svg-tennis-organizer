using FluentValidation;
using TennisClub.Api.Common.Endpoints;

namespace TennisClub.Api.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/auth/forgot-password", async (
            ForgotPasswordRequest req,
            IValidator<ForgotPasswordRequest> validator,
            ForgotPasswordHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            await handler.HandleAsync(req, ct);
            // Always OK - enumeration protection.
            return Results.Ok(new
            {
                message = "Falls die Adresse registriert ist, haben wir dir einen Link geschickt."
            });
        })
        .AllowAnonymous()
        .RequireRateLimiting("auth-forgot");
}
