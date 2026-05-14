using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using TennisClub.Api.Common.Endpoints;

namespace TennisClub.Api.Features.Auth.Register;

public sealed class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/auth/register", async (
            RegisterRequest req,
            IValidator<RegisterRequest> validator,
            RegisterHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            await handler.Handle(req, ct);

            // Generic OK regardless of whether the address was new, taken
            // or rejected by identity. Prevents enumeration.
            return Results.Ok(new
            {
                message = "Falls die Adresse gültig ist, ist eine Mail mit dem "
                    + "Link zum Passwort-Setzen unterwegs."
            });
        })
        .RequireRateLimiting("auth-register");
}
