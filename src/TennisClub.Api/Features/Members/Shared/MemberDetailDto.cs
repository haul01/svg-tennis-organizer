namespace TennisClub.Api.Features.Members.Shared;

public sealed record MemberDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);
