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

    /// <summary>
    /// True when the booking includes a guest player. The named guest
    /// (GuestPlayerId) is optional and may stay null until the guest-name
    /// flow ships; this flag is the lightweight billing signal.
    /// </summary>
    public bool HasGuest { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
