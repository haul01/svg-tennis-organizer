using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Common.Exceptions;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Common.Time;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Features.Reservations.Rules;
using TennisClub.Api.Infrastructure.Email;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.Create;

public sealed class CreateReservationHandler(
    AppDbContext db,
    BookingRuleEngine rules,
    EmailQueue email,
    EmailTemplateRenderer templates,
    TimeProvider time)
{
    public async Task<Result<Guid>> HandleAsync(
        CreateReservationRequest req, Guid memberId, CancellationToken ct)
    {
        if (req.GuestPlayerId is Guid guestId)
        {
            var guestOk = await db.GuestPlayers.AnyAsync(
                g => g.Id == guestId && g.InvitedByMemberId == memberId && g.IsActive, ct);
            if (!guestOk)
            {
                return Result.Invalid(new List<ValidationFailure>
                {
                    new("GUEST_INVALID", "Der ausgewählte Gastspieler ist nicht verfügbar.")
                });
            }
        }

        var attempt = new BookingAttempt(memberId, req.CourtId, req.StartsAt, req.EndsAt);

        // Layer 1: rule engine reports all violations in one shot.
        var failures = await rules.CheckAsync(attempt, ct);
        if (failures.Count > 0)
        {
            var asValidation = failures
                .Select(f => new ValidationFailure(f.Code!, f.Message!))
                .ToList();
            return Result.Invalid(asValidation);
        }

        // Layer 2: filtered unique index is the ultimate defense against
        // concurrent double-bookings. Anything the rules missed will surface here.
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CourtId = req.CourtId,
            MemberId = memberId,
            GuestPlayerId = req.GuestPlayerId,
            // A picked guest implies the booking has a guest, even if the
            // client forgot the boolean.
            HasGuest = req.HasGuest || req.GuestPlayerId is not null,
            StartsAt = req.StartsAt,
            EndsAt = req.EndsAt,
            Status = ReservationStatus.Active,
            CreatedAt = time.GetUtcNow()
        };
        db.Reservations.Add(reservation);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            return Result.Conflict(
                "Der Slot wurde gerade von jemand anderem gebucht.");
        }

        // Best-effort: a mail-pipeline hiccup must not roll back a successful
        // booking. The dispatcher will log render or SMTP failures separately.
        try { await SendConfirmationAsync(reservation, ct); }
        catch { /* swallowed on purpose */ }

        return Result.Success(reservation.Id);
    }

    private async Task SendConfirmationAsync(Reservation r, CancellationToken ct)
    {
        // Pull the bits the template needs in a single query so the
        // confirmation mail keeps a stable shape regardless of caller.
        var ctx = await db.Reservations
            .AsNoTracking()
            .Where(x => x.Id == r.Id)
            .Select(x => new
            {
                x.Member.Email,
                x.Member.FirstName,
                CourtName = x.Court.Name,
                GuestName = x.GuestPlayer != null
                    ? (x.GuestPlayer.FirstName + " " + x.GuestPlayer.LastName)
                    : null
            })
            .FirstAsync(ct);

        var localStart = ClubTimeZone.LocalDateTime(r.StartsAt);
        var localEnd = ClubTimeZone.LocalDateTime(r.EndsAt);

        var html = await templates.RenderAsync("booking-confirmation", new
        {
            FirstName = ctx.FirstName,
            CourtName = ctx.CourtName,
            DateLabel = localStart.ToString("dddd, d. MMMM yyyy",
                System.Globalization.CultureInfo.GetCultureInfo("de-AT")),
            TimeLabel = $"{localStart:HH:mm} – {localEnd:HH:mm} Uhr",
            HasGuest = r.HasGuest,
            GuestName = ctx.GuestName
        }, ct);

        await email.EnqueueAsync(
            new EmailMessage(ctx.Email!, "Buchungsbestätigung", html), ct);
    }
}
