namespace TennisClub.Api.Features.CourtBlocks.CreateOnce;

public sealed record CreateOnceRequest(
    int CourtId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Reason,
    bool ForceCancelConflicts);
