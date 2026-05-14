using FluentValidation;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Common.Results;

namespace TennisClub.Api.Features.Admin.Diag.SendTestEmail;

public sealed class SendTestEmailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/admin/diag/test-email", async (
            SendTestEmailRequest req,
            IValidator<SendTestEmailRequest> validator,
            SendTestEmailHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(req, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization("Admin");
}
