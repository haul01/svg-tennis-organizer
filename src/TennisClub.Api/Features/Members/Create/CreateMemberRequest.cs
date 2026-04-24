namespace TennisClub.Api.Features.Members.Create;

public sealed record CreateMemberRequest(
    string FirstName,
    string LastName,
    string Email,
    string Role);
