namespace TennisClub.Api.Features.Reservations.Rules;

public sealed record BookingAttempt(
    Guid MemberId,
    int CourtId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);
