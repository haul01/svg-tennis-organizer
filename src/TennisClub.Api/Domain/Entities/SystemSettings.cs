namespace TennisClub.Api.Domain.Entities;

public class SystemSettings
{
    public int Id { get; set; }
    public int MaxAdvanceBookingDays { get; set; } = 7;
    public int MinCancellationHours { get; set; } = 0;
    public int MaxOpenReservationsPerMember { get; set; } = 2;
    /// <summary>
    /// Hard cap on how many consecutive slot units a single reservation can
    /// span. With 30-min slots this defaults to 4 (= 2 h max per booking).
    /// </summary>
    public int MaxSlotsPerBooking { get; set; } = 4;
}
