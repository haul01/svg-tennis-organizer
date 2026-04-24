using FluentAssertions;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Features.Reservations.ListForWeek;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Reservations;

public class ListForWeekHandlerTests
{
    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-06-15T10:00:00+02:00");

    // Monday 08.06.2026 is the start of the week containing Now.
    private static readonly DateTimeOffset WeekStart =
        DateTimeOffset.Parse("2026-06-08T00:00:00+02:00");

    [Fact]
    public async Task Week_IncludesOnlyActiveReservationsInWindow()
    {
        await using var host = new RuleTestHost(Now);
        host.AddCourt();
        var member = host.AddMember();

        // In window.
        host.AddReservation(1, member.Id,
            WeekStart.AddDays(1).AddHours(18),
            WeekStart.AddDays(1).AddHours(19));
        // Cancelled in window - must be excluded.
        host.AddReservation(1, member.Id,
            WeekStart.AddDays(2).AddHours(18),
            WeekStart.AddDays(2).AddHours(19),
            status: ReservationStatus.Cancelled);
        // Previous week.
        host.AddReservation(1, member.Id,
            WeekStart.AddDays(-2).AddHours(18),
            WeekStart.AddDays(-2).AddHours(19));
        // Next week.
        host.AddReservation(1, member.Id,
            WeekStart.AddDays(8).AddHours(18),
            WeekStart.AddDays(8).AddHours(19));

        var handler = new ListForWeekHandler(host.Db);
        var result = await handler.HandleAsync(WeekStart, member.Id, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].IsMine.Should().BeTrue();
    }

    [Fact]
    public async Task Week_OtherMembersReservations_AreOpaqueWithoutGuestName()
    {
        await using var host = new RuleTestHost(Now);
        host.AddCourt();
        var me = host.AddMember();
        var someoneElse = host.AddMember();

        var mine = host.AddReservation(1, me.Id,
            WeekStart.AddDays(1).AddHours(18),
            WeekStart.AddDays(1).AddHours(19));

        var theirs = host.AddReservation(1, someoneElse.Id,
            WeekStart.AddDays(1).AddHours(19),
            WeekStart.AddDays(1).AddHours(20));

        var handler = new ListForWeekHandler(host.Db);
        var result = await handler.HandleAsync(WeekStart, me.Id, CancellationToken.None);

        result.Should().HaveCount(2);

        var mineDto = result.Single(r => r.Id == mine.Id);
        mineDto.IsMine.Should().BeTrue();

        var theirsDto = result.Single(r => r.Id == theirs.Id);
        theirsDto.IsMine.Should().BeFalse();
        theirsDto.GuestName.Should().BeNull("foreign guest names leak PII");
    }
}
