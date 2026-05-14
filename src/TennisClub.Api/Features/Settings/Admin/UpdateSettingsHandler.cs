using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Features.Settings.Public;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Settings.Admin;

public sealed class UpdateSettingsHandler(AppDbContext db)
{
    public async Task<Result<PublicSettingsDto>> HandleAsync(
        UpdateSettingsRequest req, CancellationToken ct)
    {
        var settings = await db.SystemSettings.FirstOrDefaultAsync(ct);
        if (settings is null)
            return Result.NotFound("Systemeinstellungen wurden nicht initialisiert.");

        settings.MaxAdvanceBookingDays = req.MaxAdvanceBookingDays;
        settings.MinCancellationHours = req.MinCancellationHours;
        settings.MaxOpenReservationsPerMember = req.MaxOpenReservationsPerMember;
        settings.MaxSlotsPerBooking = req.MaxSlotsPerBooking;
        settings.GuestMembershipPromptText = req.GuestMembershipPromptText.Trim();

        await db.SaveChangesAsync(ct);

        return Result.Success(new PublicSettingsDto(
            settings.MaxAdvanceBookingDays,
            settings.MinCancellationHours,
            settings.MaxOpenReservationsPerMember,
            settings.MaxSlotsPerBooking,
            settings.GuestMembershipPromptText));
    }
}
