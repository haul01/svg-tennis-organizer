namespace TennisClub.Api.Features.Profile.ChangePassword;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
