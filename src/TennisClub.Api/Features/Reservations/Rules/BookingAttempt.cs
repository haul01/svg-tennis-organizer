namespace TennisClub.Api.Features.Reservations.Rules;

public sealed record BookingAttempt(
    Guid MemberId,
    int CourtId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    // Roles of the booking member, looked up once in the handler so rules
    // can run purely against in-memory data instead of pulling Identity
    // tables. Empty when role-based logic isn't relevant for a test.
    IReadOnlyCollection<string> Roles = null!)
{
    public IReadOnlyCollection<string> Roles { get; init; } = Roles ?? Array.Empty<string>();
}
