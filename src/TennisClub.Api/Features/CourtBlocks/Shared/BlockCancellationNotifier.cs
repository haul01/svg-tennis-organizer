using TennisClub.Api.Common.Time;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Infrastructure.Email;

namespace TennisClub.Api.Features.CourtBlocks.Shared;

/// <summary>
/// Sends a "your booking was cancelled due to a court block" mail to each
/// affected member after a force-cancel. Best-effort by design: the
/// reservations are already marked cancelled in the DB, so a mail-pipeline
/// failure must not roll that back. Errors are logged + swallowed; the
/// EmailDispatcher will report transport failures on its own.
/// </summary>
public sealed class BlockCancellationNotifier(
    EmailQueue email,
    EmailTemplateRenderer templates,
    ILogger<BlockCancellationNotifier> log)
{
    public async Task NotifyCancelledAsync(
        IEnumerable<Reservation> cancelled,
        string blockReason,
        CancellationToken ct)
    {
        foreach (var r in cancelled)
        {
            if (string.IsNullOrWhiteSpace(r.Member?.Email)) continue;

            var localStart = ClubTimeZone.LocalDateTime(r.StartsAt);
            var localEnd = ClubTimeZone.LocalDateTime(r.EndsAt);

            try
            {
                var rendered = await templates.RenderEmailAsync(
                    "booking-cancelled-by-admin",
                    new
                    {
                        FirstName = r.Member.FirstName,
                        CourtName = r.Court?.Name ?? "Platz",
                        DateLabel = localStart.ToString("dddd, d. MMMM yyyy",
                            System.Globalization.CultureInfo.GetCultureInfo("de-AT")),
                        TimeLabel = $"{localStart:HH:mm} – {localEnd:HH:mm} Uhr",
                        BlockReason = blockReason
                    },
                    ct);

                await email.EnqueueAsync(
                    new EmailMessage(
                        r.Member.Email!,
                        "Buchung storniert – Platz wurde gesperrt",
                        rendered.Html, rendered.Plain),
                    ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex,
                    "Failed to enqueue admin-cancel mail for reservation {ReservationId} to {Email}",
                    r.Id, r.Member.Email);
            }
        }
    }
}
