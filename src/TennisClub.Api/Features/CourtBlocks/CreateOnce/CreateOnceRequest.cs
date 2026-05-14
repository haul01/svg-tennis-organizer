namespace TennisClub.Api.Features.CourtBlocks.CreateOnce;

public sealed record CreateOnceRequest(
    int CourtId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Reason,
    bool ForceCancelConflicts,
    /// <summary>
    /// When true, the block applies to every active court. CourtId is
    /// ignored. One CourtBlock row is materialized per active court,
    /// all sharing one SeriesId so the admin can delete the group.
    /// </summary>
    bool AllCourts = false);
