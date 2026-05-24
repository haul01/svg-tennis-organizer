using Microsoft.AspNetCore.Http;
using TennisClub.Api.Common.Endpoints;

namespace TennisClub.Api.Features.Members.ImportCsv;

public sealed class ImportCsvEndpoint : IEndpoint
{
    // 2 MB upper bound — 50–200 members at ~60 bytes/row is well below 50 KB,
    // so anything larger is almost certainly a wrong file pasted by mistake.
    private const long MaxUploadBytes = 2 * 1024 * 1024;

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/members/import", async (
                IFormFile file,
                ImportCsvHandler handler,
                CancellationToken ct) =>
            {
                if (file is null || file.Length == 0)
                {
                    return Results.BadRequest(new { error = "Keine Datei übermittelt." });
                }

                if (file.Length > MaxUploadBytes)
                {
                    return Results.BadRequest(new { error = "Datei ist zu groß (max. 2 MB)." });
                }

                await using var stream = file.OpenReadStream();
                var summary = await handler.HandleAsync(stream, ct);
                return Results.Ok(summary);
            })
            .RequireAuthorization("Admin")
            .DisableAntiforgery();
}
