namespace TennisClub.Api.Features.Members.Update;

public sealed record UpdateMemberRequest(
    string FirstName,
    string LastName,
    string Email,
    string Role);
