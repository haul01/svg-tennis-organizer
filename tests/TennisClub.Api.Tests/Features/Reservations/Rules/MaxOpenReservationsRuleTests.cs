using FluentAssertions;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Features.Reservations.Rules;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Reservations.Rules;

public class MaxOpenReservationsRuleTests
{
    [Fact]
    public async Task UnderLimit_Passes()
    {
        await using var host = new RuleTestHost(DateTimeOffset.Parse("2026-06-15T10:00:00+02:00"));
        host.AddSystemSettings(maxOpen: 2);
        host.AddCourt();
        var member = host.AddMember();
        host.AddReservation(
            courtId: 1, memberId: member.Id,
            startsAt: DateTimeOffset.Parse("2026-06-16T18:00:00+02:00"),
            endsAt: DateTimeOffset.Parse("2026-06-16T19:00:00+02:00"));

        var rule = new MaxOpenReservationsRule(host.Db, host.Time);
        var result = await rule.CheckAsync(
            new BookingAttempt(member.Id, 1,
                DateTimeOffset.Parse("2026-06-17T18:00:00+02:00"),
                DateTimeOffset.Parse("2026-06-17T19:00:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AtLimit_Fails()
    {
        await using var host = new RuleTestHost(DateTimeOffset.Parse("2026-06-15T10:00:00+02:00"));
        host.AddSystemSettings(maxOpen: 2);
        host.AddCourt();
        var member = host.AddMember();
        host.AddReservation(1, member.Id,
            DateTimeOffset.Parse("2026-06-16T18:00:00+02:00"),
            DateTimeOffset.Parse("2026-06-16T19:00:00+02:00"));
        host.AddReservation(1, member.Id,
            DateTimeOffset.Parse("2026-06-17T18:00:00+02:00"),
            DateTimeOffset.Parse("2026-06-17T19:00:00+02:00"));

        var rule = new MaxOpenReservationsRule(host.Db, host.Time);
        var result = await rule.CheckAsync(
            new BookingAttempt(member.Id, 1,
                DateTimeOffset.Parse("2026-06-18T18:00:00+02:00"),
                DateTimeOffset.Parse("2026-06-18T19:00:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("TOO_MANY_OPEN");
    }

    [Fact]
    public async Task PastReservations_DoNotCount()
    {
        await using var host = new RuleTestHost(DateTimeOffset.Parse("2026-06-15T10:00:00+02:00"));
        host.AddSystemSettings(maxOpen: 2);
        host.AddCourt();
        var member = host.AddMember();
        // Two old + finished reservations - must not count towards the limit.
        host.AddReservation(1, member.Id,
            DateTimeOffset.Parse("2026-05-10T18:00:00+02:00"),
            DateTimeOffset.Parse("2026-05-10T19:00:00+02:00"));
        host.AddReservation(1, member.Id,
            DateTimeOffset.Parse("2026-05-12T18:00:00+02:00"),
            DateTimeOffset.Parse("2026-05-12T19:00:00+02:00"));

        var rule = new MaxOpenReservationsRule(host.Db, host.Time);
        var result = await rule.CheckAsync(
            new BookingAttempt(member.Id, 1,
                DateTimeOffset.Parse("2026-06-18T18:00:00+02:00"),
                DateTimeOffset.Parse("2026-06-18T19:00:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CancelledReservations_DoNotCount()
    {
        await using var host = new RuleTestHost(DateTimeOffset.Parse("2026-06-15T10:00:00+02:00"));
        host.AddSystemSettings(maxOpen: 2);
        host.AddCourt();
        var member = host.AddMember();
        host.AddReservation(1, member.Id,
            DateTimeOffset.Parse("2026-06-16T18:00:00+02:00"),
            DateTimeOffset.Parse("2026-06-16T19:00:00+02:00"),
            status: ReservationStatus.Cancelled);
        host.AddReservation(1, member.Id,
            DateTimeOffset.Parse("2026-06-17T18:00:00+02:00"),
            DateTimeOffset.Parse("2026-06-17T19:00:00+02:00"),
            status: ReservationStatus.Cancelled);

        var rule = new MaxOpenReservationsRule(host.Db, host.Time);
        var result = await rule.CheckAsync(
            new BookingAttempt(member.Id, 1,
                DateTimeOffset.Parse("2026-06-18T18:00:00+02:00"),
                DateTimeOffset.Parse("2026-06-18T19:00:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }
}
