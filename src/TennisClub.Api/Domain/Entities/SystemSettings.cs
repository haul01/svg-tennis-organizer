namespace TennisClub.Api.Domain.Entities;

public class SystemSettings
{
    public int Id { get; set; }
    public int MaxAdvanceBookingDays { get; set; } = 7;
    public int MinCancellationHours { get; set; } = 2;
    public int MaxOpenReservationsPerMember { get; set; } = 2;
}
