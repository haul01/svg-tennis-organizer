using TennisClub.Api.Common.Results;
using TennisClub.Api.Infrastructure.Email;

namespace TennisClub.Api.Features.Admin.Diag.SendTestEmail;

/// <summary>
/// Admin-only smoke test: sends a minimal mail straight through the
/// configured <see cref="IEmailSender"/>, bypassing the EmailQueue so we
/// can surface SMTP errors synchronously to the caller.
/// </summary>
public sealed class SendTestEmailHandler(
    IEmailSender sender,
    ILogger<SendTestEmailHandler> log)
{
    public async Task<Result> HandleAsync(SendTestEmailRequest req, CancellationToken ct)
    {
        var message = new EmailMessage(
            To: req.To,
            Subject: "SVG Tennis Test-Mail",
            HtmlBody: """
                <!DOCTYPE html>
                <html lang="de"><head><meta charset="UTF-8"></head>
                <body style="font-family: Arial, sans-serif; color: #0b1c30; line-height: 1.6;">
                  <h1 style="color: #0a192f;">Test-Mail</h1>
                  <p>Wenn diese Nachricht angekommen ist, funktioniert der
                  Mail-Versand korrekt - SMTP-Verbindung, Auth und DKIM sind
                  in Ordnung.</p>
                  <p style="color: #44474d; font-size: 14px;">
                    Manuell ausgelöst über den Admin-Diag-Endpoint.
                  </p>
                </body></html>
                """,
            PlainTextBody: """
                Test-Mail

                Wenn diese Nachricht angekommen ist, funktioniert der
                Mail-Versand korrekt - SMTP-Verbindung, Auth und DKIM sind
                in Ordnung.

                Manuell ausgelöst über den Admin-Diag-Endpoint.
                """);

        try
        {
            await sender.SendAsync(message, ct);
            log.LogInformation("Diag test email sent to {To}", req.To);
            return Result.Success();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Surface the actual SMTP error so admins can debug auth, DKIM,
            // blocked port, etc. without digging through the container log.
            log.LogError(ex, "Diag test email to {To} failed", req.To);
            return Result.Invalid($"SMTP-Versand fehlgeschlagen: {ex.Message}");
        }
    }
}
