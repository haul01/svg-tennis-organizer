namespace TennisClub.Api.Features.Auth.ResetPassword;

public sealed record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword);
