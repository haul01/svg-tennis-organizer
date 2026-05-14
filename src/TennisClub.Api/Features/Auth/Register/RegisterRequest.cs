namespace TennisClub.Api.Features.Auth.Register;

/// <summary>
/// Public self-registration as a Guest. Password is NOT collected here -
/// the welcome mail carries a set-password link, which both verifies
/// the address and gates account activation.
/// </summary>
public sealed record RegisterRequest(
    string Email,
    string FirstName,
    string LastName);
