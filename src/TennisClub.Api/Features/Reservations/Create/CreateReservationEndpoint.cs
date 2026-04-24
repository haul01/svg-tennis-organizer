using System.Security.Claims;
using FluentValidation;
using Http = Microsoft.AspNetCore.Http;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Reservations.Create;

public sealed class CreateReservationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/reservations", async (
            CreateReservationRequest req,
            IValidator<CreateReservationRequest> validator,
            CreateReservationHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var memberId = user.GetMemberId();
            var result = await handler.HandleAsync(req, memberId, ct);

            return result.ToHttpResult(
                onSuccess: id => Http.Results.Created($"/api/reservations/{id}", new { id }));
        })
        .RequireAuthorization();
}
