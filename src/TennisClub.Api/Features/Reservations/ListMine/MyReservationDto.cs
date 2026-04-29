using TennisClub.Api.Domain.Enums;

namespace TennisClub.Api.Features.Reservations.ListMine;

public sealed record MyReservationDto(
    Guid Id,
    int CourtId,
    string CourtName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    ReservationStatus Status,
    DateTimeOffset? CancelledAt,
    bool HasGuest,
    string? GuestName);
