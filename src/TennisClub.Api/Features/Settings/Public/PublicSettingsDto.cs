namespace TennisClub.Api.Features.Settings.Public;

public sealed record PublicSettingsDto(
    int MaxAdvanceBookingDays,
    int MinCancellationHours,
    int MaxOpenReservationsPerMember);
