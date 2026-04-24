namespace TennisClub.Api.Features.Seasons.Update;

public sealed record UpdateSeasonRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    int SlotDurationMinutes);
