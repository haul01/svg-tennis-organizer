using TennisClub.Api.Domain.Enums;

namespace TennisClub.Api.Features.Admin.Reports.ListReservations;

/// <summary>
/// Filter + paging args for the admin reservation report. All fields
/// optional - missing dates default to "last 30 days through now+30
/// days" so the page is useful immediately on open.
/// </summary>
public sealed record ListReservationsRequest(
    DateTimeOffset? From,
    DateTimeOffset? To,
    int? CourtId,
    ReservationStatus? Status,
    int Page = 1,
    int PageSize = 25);
