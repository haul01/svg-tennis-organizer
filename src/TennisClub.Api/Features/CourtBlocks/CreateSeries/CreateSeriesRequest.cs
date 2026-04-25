namespace TennisClub.Api.Features.CourtBlocks.CreateSeries;

public sealed record CreateSeriesRequest(
    int CourtId,
    DayOfWeek Weekday,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    bool ForceCancelConflicts);
