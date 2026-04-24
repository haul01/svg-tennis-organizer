namespace TennisClub.Api.Infrastructure.Email;

/// <summary>
/// Placeholder implementation until Phase 8 wires up SMTP (Brevo) + templates.
/// Writes the message to the logger so devs can grab password-reset links
/// from the console without a real mailbox.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> log) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        log.LogInformation(
            "[DEV EMAIL] To: {To} | Subject: {Subject}\n{Body}",
            message.To, message.Subject, message.HtmlBody);
        return Task.CompletedTask;
    }
}
