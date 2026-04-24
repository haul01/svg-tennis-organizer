namespace TennisClub.Api.Features.Profile.Shared;

public sealed record ProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles);
