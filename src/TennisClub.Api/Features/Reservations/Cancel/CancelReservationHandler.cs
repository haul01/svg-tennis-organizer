using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Common.Time;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Infrastructure.Email;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.Cancel;

public sealed class CancelReservationHandler(
    AppDbContext db,
    EmailQueue email,
    EmailTemplateRenderer templates,
    TimeProvider time)
{
    public async Task<Result> HandleAsync(
        Guid reservationId,
        Guid memberId,
        byte[] rowVersion,
        CancellationToken ct)
    {
        var reservation = await db.Reservations.FindAsync([reservationId], ct);

        // Hide "not yours" behind NotFound on purpose - avoids leaking the
        // existence of other members' bookings.
        if (reservation is null || reservation.MemberId != memberId)
            return Result.NotFound("Buchung nicht gefunden.");

        if (reservation.Status != ReservationStatus.Active)
            return Result.Invalid("Diese Buchung ist bereits storniert.");

        var settings = await db.SystemSettings.FirstAsync(ct);
        var hoursUntilStart = (reservation.StartsAt - time.GetUtcNow()).TotalHours;
        if (hoursUntilStart < settings.MinCancellationHours)
        {
            return Result.Invalid(
                $"Stornierung nur bis {settings.MinCancellationHours} Stunden vor Beginn möglich.");
        }

        db.Entry(reservation).Property(r => r.RowVersion).OriginalValue = rowVersion;
        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledAt = time.GetUtcNow();

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Conflict("Die Buchung wurde inzwischen geändert, bitte neu laden.");
        }

        // Best-effort: a mail-pipeline hiccup must not undo a successful cancel.
        try { await SendCancellationAsync(reservation, ct); }
        catch { /* swallowed on purpose */ }

        return Result.Success();
    }

    private async Task SendCancellationAsync(Reservation r, CancellationToken ct)
    {
        var ctx = await db.Reservations
            .AsNoTracking()
            .Where(x => x.Id == r.Id)
            .Select(x => new
            {
                x.Member.Email,
                x.Member.FirstName,
                CourtName = x.Court.Name
            })
            .FirstAsync(ct);

        var localStart = ClubTimeZone.LocalDateTime(r.StartsAt);
        var localEnd = ClubTimeZone.LocalDateTime(r.EndsAt);

        var html = await templates.RenderAsync("booking-cancellation", new
        {
            FirstName = ctx.FirstName,
            CourtName = ctx.CourtName,
            DateLabel = localStart.ToString("dddd, d. MMMM yyyy",
                System.Globalization.CultureInfo.GetCultureInfo("de-AT")),
            TimeLabel = $"{localStart:HH:mm} – {localEnd:HH:mm} Uhr"
        }, ct);

        await email.EnqueueAsync(
            new EmailMessage(ctx.Email!, "Stornierungsbestätigung", html), ct);
    }
}
