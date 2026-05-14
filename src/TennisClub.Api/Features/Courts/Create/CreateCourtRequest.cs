namespace TennisClub.Api.Features.Courts.Create;

public sealed record CreateCourtRequest(
    string Name,
    int? DisplayOrder,
    bool IsGuestBookable = false);
