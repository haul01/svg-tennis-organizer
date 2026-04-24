using FluentAssertions;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Features.Reservations.ListMine;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Reservations;

public class ListMineHandlerTests
{
    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-06-15T10:00:00+02:00");

    [Fact]
    public async Task Mine_ReturnsOnlyOwnReservationsOrderedByStart()
    {
        await using var host = new RuleTestHost(Now);
        host.AddCourt();
        var me = host.AddMember();
        var other = host.AddMember();

        var second = host.AddReservation(1, me.Id,
            Now.AddDays(3), Now.AddDays(3).AddHours(1));
        var first = host.AddReservation(1, me.Id,
            Now.AddDays(1), Now.AddDays(1).AddHours(1));
        host.AddReservation(1, other.Id,
            Now.AddDays(2), Now.AddDays(2).AddHours(1));

        var handler = new ListMineHandler(host.Db, host.Time);
        var result = await handler.HandleAsync(me.Id,
            upcomingOnly: false, statusFilter: null, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(r => r.Id).Should().ContainInOrder(first.Id, second.Id);
        result.All(r => r.CourtName == "Platz 1").Should().BeTrue();
    }

    [Fact]
    public async Task Mine_UpcomingOnly_SkipsPastReservations()
    {
        await using var host = new RuleTestHost(Now);
        host.AddCourt();
        var me = host.AddMember();

        var upcoming = host.AddReservation(1, me.Id,
            Now.AddDays(2), Now.AddDays(2).AddHours(1));
        host.AddReservation(1, me.Id,
            Now.AddDays(-5), Now.AddDays(-5).AddHours(1));

        var handler = new ListMineHandler(host.Db, host.Time);
        var result = await handler.HandleAsync(me.Id,
            upcomingOnly: true, statusFilter: null, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(upcoming.Id);
    }

    [Fact]
    public async Task Mine_StatusFilter_IsApplied()
    {
        await using var host = new RuleTestHost(Now);
        host.AddCourt();
        var me = host.AddMember();

        host.AddReservation(1, me.Id,
            Now.AddDays(1), Now.AddDays(1).AddHours(1));
        host.AddReservation(1, me.Id,
            Now.AddDays(2), Now.AddDays(2).AddHours(1),
            status: ReservationStatus.Cancelled);

        var handler = new ListMineHandler(host.Db, host.Time);

        var active = await handler.HandleAsync(me.Id, false, ReservationStatus.Active, CancellationToken.None);
        active.Should().ContainSingle().Which.Status.Should().Be(ReservationStatus.Active);

        var cancelled = await handler.HandleAsync(me.Id, false, ReservationStatus.Cancelled, CancellationToken.None);
        cancelled.Should().ContainSingle().Which.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Fact]
    public async Task Mine_IncludesGuestName_WhenPresent()
    {
        await using var host = new RuleTestHost(Now);
        host.AddCourt();
        var me = host.AddMember();

        var guest = new GuestPlayer
        {
            Id = Guid.NewGuid(),
            FirstName = "Max",
            LastName = "Mustermann",
            InvitedByMemberId = me.Id,
            CreatedAt = Now,
            IsActive = true
        };
        host.Db.GuestPlayers.Add(guest);
        host.Db.SaveChanges();

        var reservation = host.AddReservation(1, me.Id,
            Now.AddDays(1), Now.AddDays(1).AddHours(1));
        reservation.GuestPlayerId = guest.Id;
        host.Db.SaveChanges();

        var handler = new ListMineHandler(host.Db, host.Time);
        var result = await handler.HandleAsync(me.Id, false, null, CancellationToken.None);

        result.Should().ContainSingle().Which.GuestName.Should().Be("Max Mustermann");
    }
}
