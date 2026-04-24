namespace TennisClub.Api.Domain.Entities;

public class CourtBlock
{
    public Guid Id { get; set; }

    public int CourtId { get; set; }
    public Court Court { get; set; } = null!;

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string Reason { get; set; } = null!;

    public Guid? SeriesId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedByMemberId { get; set; }
}
