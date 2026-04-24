namespace TennisClub.Api.Features.Seasons.Current;

public sealed record SeasonDto(
    int Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    int SlotDurationMinutes);
