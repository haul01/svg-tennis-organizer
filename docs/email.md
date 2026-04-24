# E-Mail-Versand

Transaktionale E-Mails (Buchungsbestätigung, Stornierung, Passwort-Reset) via Brevo SMTP.

## Provider-Wahl: Brevo

**Volumen-Schätzung:**
- MVP: ~2.000 Mails/Monat (Buchungen + Stornierungen + First-Login)
- V2 mit Erinnerungs-Mails: ~3.700 Mails/Monat

**Brevo Free Tier:** 300 Mails/Tag = 9.000/Monat, dauerhaft kostenlos, EU-basiert (Paris), GDPR-konform.

Alternativen dokumentiert, falls Brevo wegfällt:
- Resend (3.000/Monat, US-Server)
- Mailjet (6.000/Monat, EU)
- Azure Communication Services (pay-as-you-go, ~0,20€/Monat für unser Volumen)

## Domain-Setup (einmalig)

**Was wir brauchen:** Eine Vereinsdomain mit DNS-Zugriff (`tennisverein.at` oder ähnlich).

**Was Brevo ist / nicht ist:** Brevo **versendet** E-Mails im Namen einer Adresse, die uns gehört. Postfächer / Empfang macht Brevo nicht.

**Empfangs-Strategie:** Falls der Verein schon Postfächer beim Webhoster hat → Absender `reservierung@verein.at` einfach auf bestehendes Postfach lenken. Falls nicht → **ImprovMX** oder **ForwardEmail.net** als kostenlose Mail-Weiterleitung einrichten: `reservierung@verein.at` → `verein-gmail@gmail.com`.

**DKIM/SPF/DMARC:** Pflicht für Deliverability. Brevo zeigt die drei DNS-Einträge im Admin-Panel nach Domain-Verifikation. Einmal einrichten, dann läuft's.

## Implementation

### NuGet

```xml
<PackageReference Include="MailKit" Version="4.*" />
<PackageReference Include="Scriban" Version="5.*" />
```

MailKit statt `System.Net.Mail.SmtpClient` — Letzteres ist von Microsoft als obsolet markiert.

### Konfiguration

`appsettings.json` (Dev; in Prod aus Container App Secrets):

```json
{
  "Smtp": {
    "Host": "smtp-relay.brevo.com",
    "Port": 587,
    "Username": "<brevo-smtp-user>",
    "Password": "<brevo-smtp-key>",
    "FromName": "TennisClub",
    "FromAddress": "reservierung@tennisverein.at"
  }
}
```

```csharp
public sealed class SmtpSettings
{
    public string Host { get; set; } = null!;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FromName { get; set; } = null!;
    public string FromAddress { get; set; } = null!;
}
```

### IEmailSender + Implementierung

```csharp
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null);

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

        var builder = new BodyBuilder { HtmlBody = message.HtmlBody };
        if (message.PlainTextBody is not null) builder.TextBody = message.PlainTextBody;
        mime.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(true, ct);

        log.LogInformation("Email sent to {To} with subject {Subject}", message.To, message.Subject);
    }
}
```

### EmailQueue + BackgroundService

E-Mails dürfen nicht synchron im Request-Handler versendet werden — SMTP dauert 200–800ms, bei Netzwerkproblemen sekunden. Der User wartet sonst auf die Mail statt auf seine Buchungsbestätigung.

```csharp
public sealed class EmailQueue
{
    private readonly Channel<EmailMessage> _channel =
        Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    public ValueTask EnqueueAsync(EmailMessage msg, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(msg, ct);

    public IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}

public sealed class EmailDispatcher(
    EmailQueue queue, IEmailSender sender, ILogger<EmailDispatcher> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var msg in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await sender.SendAsync(msg, stoppingToken);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to send email to {To}", msg.To);
                // Optional: in eine failed_emails-Tabelle für Retry schreiben
            }
        }
    }
}
```

Registrierung in `Program.cs`:

```csharp
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<EmailDispatcher>();
```

Im Handler dann:

```csharp
await _queue.EnqueueAsync(new EmailMessage(
    member.Email!,
    "Buchungsbestätigung",
    htmlBody), ct);
```

**Einschränkung:** Bei Container-Neustart gehen ungesendete Mails aus dem In-Memory-Channel verloren. Für kritische Mails (Passwort-Reset) lieber synchron versenden. Für Buchungsbestätigungen akzeptabel.

## Templates mit Scriban

Templates als `.sbn`-Dateien in `Infrastructure/Email/Templates/`. Scriban ist leichtgewichtig (keine MVC/Razor-Infrastruktur nötig).

### Beispiel: booking-confirmation.sbn

```
<!DOCTYPE html>
<html lang="de">
<head><meta charset="UTF-8"></head>
<body style="font-family: Arial, sans-serif; color: #333;">
  <h1>Hallo {{ first_name }},</h1>

  <p>deine Reservierung ist bestätigt:</p>

  <table style="border-collapse: collapse; margin: 20px 0;">
    <tr>
      <td style="padding: 8px 16px 8px 0;"><strong>Platz:</strong></td>
      <td style="padding: 8px 0;">{{ court_name }}</td>
    </tr>
    <tr>
      <td style="padding: 8px 16px 8px 0;"><strong>Datum:</strong></td>
      <td style="padding: 8px 0;">{{ starts_at | date.to_string "%A, %d.%m.%Y" }}</td>
    </tr>
    <tr>
      <td style="padding: 8px 16px 8px 0;"><strong>Uhrzeit:</strong></td>
      <td style="padding: 8px 0;">{{ starts_at | date.to_string "%H:%M" }} – {{ ends_at | date.to_string "%H:%M" }}</td>
    </tr>
    {{ if guest_name }}
    <tr>
      <td style="padding: 8px 16px 8px 0;"><strong>Gastspieler:</strong></td>
      <td style="padding: 8px 0;">{{ guest_name }}</td>
    </tr>
    {{ end }}
  </table>

  {{ if guest_name }}
  <p><em>Bitte vergiss nicht, die Gastspielergebühr im Vereinsheim zu entrichten.</em></p>
  {{ end }}

  <p>Sportliche Grüße<br>Dein TennisClub</p>
</body>
</html>
```

### Renderer

```csharp
public sealed class EmailTemplateRenderer
{
    private readonly string _templateDir;

    public EmailTemplateRenderer()
    {
        _templateDir = Path.Combine(
            AppContext.BaseDirectory,
            "Infrastructure", "Email", "Templates");
    }

    public async Task<string> RenderAsync<T>(string templateName, T model)
    {
        var path = Path.Combine(_templateDir, $"{templateName}.sbn");
        var source = await File.ReadAllTextAsync(path);
        var template = Scriban.Template.Parse(source);
        return await template.RenderAsync(model, memberRenamer: m => m.Name.ToSnakeCase());
    }
}
```

Wichtig: `.sbn`-Dateien im `.csproj` als `<Content>` mit `CopyToOutputDirectory="PreserveNewest"` markieren, damit sie ins Build-Output kommen.

```xml
<ItemGroup>
  <Content Include="Infrastructure/Email/Templates/*.sbn">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

## Template-Inventar (MVP)

- `booking-confirmation.sbn` — nach Create Reservation
- `booking-cancellation.sbn` — nach Cancel Reservation
- `welcome.sbn` — nach Admin-Anlage eines Mitglieds (inkl. Passwort-Setzen-Link)
- `password-reset.sbn` — nach ForgotPassword-Request

## V2-Templates

- `booking-reminder.sbn` — 24h vor Spielbeginn
- `partner-search-*.sbn` — für Partnersuche-Feature

## Auslösung im Handler

```csharp
public sealed class CreateReservationHandler(
    /* ... */
    EmailQueue emails,
    EmailTemplateRenderer renderer)
{
    public async Task<Result<Guid>> HandleAsync(/* ... */)
    {
        // ... Regeln + Insert
        
        var html = await renderer.RenderAsync("booking-confirmation", new
        {
            FirstName = member.FirstName,
            CourtName = court.Name,
            StartsAt = reservation.StartsAt,
            EndsAt = reservation.EndsAt,
            GuestName = guest?.FirstName + " " + guest?.LastName
        });

        await emails.EnqueueAsync(new EmailMessage(
            member.Email!,
            "Buchungsbestätigung",
            html), ct);

        return Result.Success(reservation.Id);
    }
}
```

## Testing

- **Unit-Test des `EmailTemplateRenderer`** mit Sample-Models, verifiziert dass alle Variablen korrekt ersetzt werden
- **Integration-Test nicht mit echtem SMTP** — stattdessen `IEmailSender` im Test mit einem In-Memory-Fake ersetzen, der die Nachrichten in einer Liste sammelt

```csharp
public sealed class InMemoryEmailSender : IEmailSender
{
    public List<EmailMessage> SentMessages { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        SentMessages.Add(message);
        return Task.CompletedTask;
    }
}
```

## Deliverability-Checkliste

- [ ] Eigene Domain verifiziert in Brevo
- [ ] SPF-Eintrag gesetzt
- [ ] DKIM-Eintrag gesetzt
- [ ] DMARC-Eintrag gesetzt (mindestens `p=none` zum Start)
- [ ] Absender-Adresse mit gültigem MX (kein reines No-Reply ohne Empfang)
- [ ] Plain-Text-Variante neben HTML
- [ ] Erste Test-Mails an verschiedene Provider senden (GMX, Gmail, Outlook) und Spam-Ordner checken
