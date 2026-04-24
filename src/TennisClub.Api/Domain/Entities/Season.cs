namespace TennisClub.Api.Domain.Entities;

public class Season
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TimeOnly OpeningTime { get; set; }
    public TimeOnly ClosingTime { get; set; }
    public int SlotDurationMinutes { get; set; } = 60;
}
