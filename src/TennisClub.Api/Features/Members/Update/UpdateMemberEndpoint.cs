using System.Security.Claims;
using FluentValidation;
using TennisClub.Api.Common.Auth;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Members.Update;

public sealed class UpdateMemberEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("/api/members/{id:guid}", async (
            Guid id,
            UpdateMemberRequest req,
            IValidator<UpdateMemberRequest> validator,
            UpdateMemberHandler handler,
            ClaimsPrincipal caller,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(id, req, caller.GetMemberId(), ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization("Admin");
}
