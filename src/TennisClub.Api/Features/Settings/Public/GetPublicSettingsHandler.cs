using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Features.Settings.Public;

public sealed class GetPublicSettingsHandler(AppDbContext db)
{
    public async Task<PublicSettingsDto> HandleAsync(CancellationToken ct)
    {
        var settings = await db.SystemSettings.AsNoTracking().FirstAsync(ct);
        return new PublicSettingsDto(
            settings.MaxAdvanceBookingDays,
            settings.MinCancellationHours,
            settings.MaxOpenReservationsPerMember,
            settings.MaxSlotsPerBooking,
            settings.GuestMembershipPromptText);
    }
}
