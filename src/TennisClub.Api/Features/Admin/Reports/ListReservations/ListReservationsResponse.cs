namespace TennisClub.Api.Features.Admin.Reports.ListReservations;

public sealed record ListReservationsResponse(
    IReadOnlyList<ReservationReportItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
