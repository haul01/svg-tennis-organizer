namespace TennisClub.Api.Features.Members.ImportCsv;

public sealed record ImportCsvRowError(int LineNumber, string? Email, string Message);

public sealed record ImportCsvSummary(
    int TotalRows,
    int Created,
    IReadOnlyList<string> SkippedEmails,
    IReadOnlyList<ImportCsvRowError> Failed);
