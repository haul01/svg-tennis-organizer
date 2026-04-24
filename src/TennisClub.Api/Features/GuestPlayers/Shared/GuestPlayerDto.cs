namespace TennisClub.Api.Features.GuestPlayers.Shared;

public sealed record GuestPlayerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    bool IsActive,
    DateTimeOffset CreatedAt);
