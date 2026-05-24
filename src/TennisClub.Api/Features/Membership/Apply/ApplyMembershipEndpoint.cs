using FluentValidation;
using TennisClub.Api.Common.Endpoints;

namespace TennisClub.Api.Features.Membership.Apply;

public sealed class ApplyMembershipEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/membership/apply", async (
                ApplyMembershipRequest req,
                IValidator<ApplyMembershipRequest> validator,
                ApplyMembershipHandler handler,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(req, ct);
                if (!validation.IsValid)
                    return Results.ValidationProblem(validation.ToDictionary());

                await handler.HandleAsync(req, ct);
                return Results.Ok(new
                {
                    message = "Vielen Dank! Deine Beitrittserklärung ist bei uns eingegangen."
                });
            })
            .AllowAnonymous()
            .RequireRateLimiting("membership-apply");
}
