using System.ComponentModel.DataAnnotations;
using TennisClub.Api.Domain.Enums;

namespace TennisClub.Api.Domain.Entities;

public class Reservation
{
    public Guid Id { get; set; }

    public int CourtId { get; set; }
    public Court Court { get; set; } = null!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public Guid? GuestPlayerId { get; set; }
    public GuestPlayer? GuestPlayer { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
