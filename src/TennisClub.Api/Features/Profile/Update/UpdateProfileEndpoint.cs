using System.Security.Claims;
using FluentValidation;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Profile.Update;

public sealed class UpdateProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("/api/profile", async (
            UpdateProfileRequest req,
            IValidator<UpdateProfileRequest> validator,
            UpdateProfileHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(user.GetMemberId(), req, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization();
}
