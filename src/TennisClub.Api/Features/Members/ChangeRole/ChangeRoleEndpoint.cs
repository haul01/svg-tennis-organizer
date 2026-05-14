using FluentValidation;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Members.ChangeRole;

public sealed class ChangeRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/members/{id:guid}/role", async (
            Guid id,
            ChangeRoleRequest req,
            IValidator<ChangeRoleRequest> validator,
            ChangeRoleHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(id, req, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization("Admin");
}
