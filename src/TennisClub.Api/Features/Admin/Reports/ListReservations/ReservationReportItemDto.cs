using TennisClub.Api.Domain.Enums;

namespace TennisClub.Api.Features.Admin.Reports.ListReservations;

public sealed record ReservationReportItemDto(
    Guid Id,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string CourtName,
    string MemberFirstName,
    string MemberLastName,
    string MemberEmail,
    /// <summary>
    /// True when the booking is marked as having a guest. May be true
    /// even with <see cref="GuestName"/> null - the named-guest flow is
    /// optional, the flag is the billing signal.
    /// </summary>
    bool HasGuest,
    string? GuestName,
    ReservationStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CancelledAt);
