using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Domain.Enums;

namespace TennisClub.Api.Features.Admin.Reports.ListReservations;

public sealed class ListReservationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/admin/reports/reservations", async (
            ListReservationsHandler handler,
            CancellationToken ct,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            int? courtId = null,
            ReservationStatus? status = null,
            int page = 1,
            int pageSize = 25) =>
        {
            var req = new ListReservationsRequest(from, to, courtId, status, page, pageSize);
            var result = await handler.HandleAsync(req, ct);
            return Results.Ok(result);
        })
        .RequireAuthorization("Admin");
}
