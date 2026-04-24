namespace TennisClub.Api.Features.Auth.Shared;

public sealed record AuthResponse(string AccessToken, string RefreshToken);
