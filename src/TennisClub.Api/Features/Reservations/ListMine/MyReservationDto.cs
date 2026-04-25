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
    string? GuestName,
    // Base64-serialized by default - the client echoes it back via If-Match
    // on cancel requests so optimistic locking works over HTTP.
    byte[] RowVersion);
