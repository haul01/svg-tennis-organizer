using FluentValidation;
using Http = Microsoft.AspNetCore.Http;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Members.Create;

public sealed class CreateMemberEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/members", async (
            CreateMemberRequest req,
            IValidator<CreateMemberRequest> validator,
            CreateMemberHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(req, ct);
            return result.ToHttpResult(
                onSuccess: m => Http.Results.Created($"/api/members/{m.Id}", m));
        })
        .RequireAuthorization("Admin");
}
