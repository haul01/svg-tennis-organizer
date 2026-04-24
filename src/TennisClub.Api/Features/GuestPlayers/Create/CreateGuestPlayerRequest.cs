namespace TennisClub.Api.Features.GuestPlayers.Create;

public sealed record CreateGuestPlayerRequest(
    string FirstName,
    string LastName,
    string? Email);
