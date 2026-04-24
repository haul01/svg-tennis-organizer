namespace TennisClub.Api.Features.Reservations.ListForWeek;

/// <summary>
/// Week-grid projection. Other members' reservations are intentionally
/// reduced to "occupied" - no names, no guest info - to stay DSGVO-safe.
/// </summary>
public sealed record WeekReservationDto(
    Guid Id,
    int CourtId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsMine,
    string? GuestName);
