namespace TennisClub.Api.Features.Reservations.Create;

public sealed record CreateReservationRequest(
    int CourtId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    Guid? GuestPlayerId,
    bool HasGuest = false);
