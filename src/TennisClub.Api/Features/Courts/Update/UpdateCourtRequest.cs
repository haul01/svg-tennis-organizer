namespace TennisClub.Api.Features.Courts.Update;

public sealed record UpdateCourtRequest(string Name, int DisplayOrder, bool IsActive);
