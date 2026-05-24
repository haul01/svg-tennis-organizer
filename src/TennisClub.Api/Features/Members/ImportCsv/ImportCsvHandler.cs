using System.Globalization;
using Microsoft.AspNetCore.Identity;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Members.Create;
using TennisClub.Api.Infrastructure.Persistence.Seed;

namespace TennisClub.Api.Features.Members.ImportCsv;

/// <summary>
/// Bulk-import members from a CSV upload (header row + Vorname;Nachname;Email).
/// Existing emails are silently skipped; everything else routes through the
/// regular CreateMemberHandler so welcome-mails and role assignment stay
/// identical to the single-member admin flow.
/// </summary>
public sealed class ImportCsvHandler(
    UserManager<Member> users,
    CreateMemberHandler createHandler)
{
    public async Task<ImportCsvSummary> HandleAsync(Stream csv, CancellationToken ct)
    {
        using var reader = new StreamReader(csv);

        var created = 0;
        var skipped = new List<string>();
        var failed = new List<ImportCsvRowError>();
        var totalRows = 0;

        var lineNumber = 0;
        var headerSeen = false;
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            // First non-empty line is the header — always skipped.
            if (!headerSeen)
            {
                headerSeen = true;
                continue;
            }

            totalRows++;

            var separator = line.Contains(';') ? ';' : ',';
            var parts = line.Split(separator);
            if (parts.Length < 3)
            {
                failed.Add(new ImportCsvRowError(lineNumber, null,
                    "Zeile hat weniger als drei Spalten."));
                continue;
            }

            var firstName = parts[0].Trim().Trim('"');
            var lastName = parts[1].Trim().Trim('"');
            var email = parts[2].Trim().Trim('"');

            if (firstName.Length == 0 || lastName.Length == 0 || email.Length == 0)
            {
                failed.Add(new ImportCsvRowError(lineNumber, email,
                    "Vorname, Nachname oder E-Mail ist leer."));
                continue;
            }

            var existing = await users.FindByEmailAsync(email);
            if (existing is not null)
            {
                skipped.Add(email);
                continue;
            }

            var req = new CreateMemberRequest(firstName, lastName, email, SeedData.MemberRole);
            var result = await createHandler.HandleAsync(req, ct);

            if (result.IsSuccess)
            {
                created++;
                continue;
            }

            var message = result.Failures is { Count: > 0 }
                ? string.Join("; ", result.Failures.Select(f => f.Message))
                : (result.Error ?? "Anlegen fehlgeschlagen.");
            failed.Add(new ImportCsvRowError(lineNumber, email, message));
        }

        return new ImportCsvSummary(totalRows, created, skipped, failed);
    }
}
