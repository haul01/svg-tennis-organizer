using System.Security.Claims;
using FluentValidation;
using Http = Microsoft.AspNetCore.Http;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.GuestPlayers.Create;

public sealed class CreateGuestPlayerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/guest-players", async (
            CreateGuestPlayerRequest req,
            IValidator<CreateGuestPlayerRequest> validator,
            CreateGuestPlayerHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var memberId = user.GetMemberId();
            var result = await handler.HandleAsync(req, memberId, ct);
            return result.ToHttpResult(
                onSuccess: guest => Http.Results.Created($"/api/guest-players/{guest.Id}", guest));
        })
        .RequireAuthorization();
}
