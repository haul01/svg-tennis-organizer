namespace TennisClub.Api.Features.Membership.Apply;

public sealed record ApplyMembershipRequest(
    string FirstName,
    string LastName,
    string Street,
    string PostalCode,
    string City,
    DateOnly BirthDate,
    string Phone,
    string Email,
    string FeeTier,
    string? Comment);
