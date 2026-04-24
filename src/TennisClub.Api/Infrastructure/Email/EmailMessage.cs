namespace TennisClub.Api.Infrastructure.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null);
