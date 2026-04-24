using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Reservations.Cancel;

public sealed class CancelReservationHandler(AppDbContext db, TimeProvider time)
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
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Conflict("Die Buchung wurde inzwischen geändert, bitte neu laden.");
        }
    }
}
