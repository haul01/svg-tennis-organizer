namespace TennisClub.Api.Features.Settings.Admin;

public sealed record UpdateSettingsRequest(
    int MaxAdvanceBookingDays,
    int MinCancellationHours,
    int MaxOpenReservationsPerMember,
    int MaxSlotsPerBooking,
    string GuestMembershipPromptText);
