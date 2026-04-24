using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Common.Exceptions;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Features.Reservations.Rules;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.Create;

public sealed class CreateReservationHandler(
    AppDbContext db,
    BookingRuleEngine rules,
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
            StartsAt = req.StartsAt,
            EndsAt = req.EndsAt,
            Status = ReservationStatus.Active,
            CreatedAt = time.GetUtcNow()
        };
        db.Reservations.Add(reservation);

        try
        {
            await db.SaveChangesAsync(ct);
            return Result.Success(reservation.Id);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            return Result.Conflict(
                "Der Slot wurde gerade von jemand anderem gebucht.");
        }
    }
}
