namespace TennisClub.Api.Features.Courts.List;

public sealed record CourtDto(
    int Id,
    string Name,
    int DisplayOrder,
    bool IsActive,
    bool IsGuestBookable);
