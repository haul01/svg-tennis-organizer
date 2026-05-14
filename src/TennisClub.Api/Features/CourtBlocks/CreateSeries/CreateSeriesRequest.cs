namespace TennisClub.Api.Features.CourtBlocks.CreateSeries;

public sealed record CreateSeriesRequest(
    int CourtId,
    DayOfWeek Weekday,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    bool ForceCancelConflicts,
    /// <summary>
    /// When true, the series applies to every active court. CourtId is
    /// ignored. The expansion produces (weeks x courts) CourtBlock rows,
    /// all sharing one SeriesId so the whole group can be deleted at once.
    /// </summary>
    bool AllCourts = false);
