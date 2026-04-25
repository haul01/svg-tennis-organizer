using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace TennisClub.Api.Infrastructure.Email;

/// <summary>
/// Production sender using MailKit + SMTP (Brevo by default per
/// docs/email.md). System.Net.Mail.SmtpClient is obsolete; MailKit is
/// the supported client for modern .NET.
/// </summary>
public sealed class SmtpEmailSender(
    IOptions<SmtpSettings> options,
    ILogger<SmtpEmailSender> log) : IEmailSender
{
    private readonly SmtpSettings _settings = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;

        var body = new BodyBuilder { HtmlBody = message.HtmlBody };
        if (message.PlainTextBody is not null) body.TextBody = message.PlainTextBody;
        mime.Body = body.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(true, ct);

        log.LogInformation("Email sent to {To} with subject {Subject}", message.To, message.Subject);
    }
}
