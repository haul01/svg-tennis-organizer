using FluentAssertions;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Features.Reservations.Rules;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Reservations.Rules;

public class NoOverlappingReservationRuleTests
{
    private static BookingAttempt Attempt() => new(
        Guid.NewGuid(), 1,
        DateTimeOffset.Parse("2026-06-15T18:00:00+02:00"),
        DateTimeOffset.Parse("2026-06-15T19:00:00+02:00"));

    [Fact]
    public async Task NoOtherReservation_Passes()
    {
        await using var host = new RuleTestHost();
        host.AddCourt();
        var rule = new NoOverlappingReservationRule(host.Db);

        var result = await rule.CheckAsync(Attempt(), CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task OverlappingActiveReservation_Fails()
    {
        await using var host = new RuleTestHost();
        host.AddCourt();
        var other = host.AddMember();
        host.AddReservation(
            courtId: 1,
            memberId: other.Id,
            startsAt: DateTimeOffset.Parse("2026-06-15T18:00:00+02:00"),
            endsAt: DateTimeOffset.Parse("2026-06-15T19:00:00+02:00"));
        var rule = new NoOverlappingReservationRule(host.Db);

        var result = await rule.CheckAsync(Attempt(), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("OVERLAP");
    }

    [Fact]
    public async Task CancelledReservation_DoesNotBlock()
    {
        await using var host = new RuleTestHost();
        host.AddCourt();
        var other = host.AddMember();
        host.AddReservation(
            courtId: 1,
            memberId: other.Id,
            startsAt: DateTimeOffset.Parse("2026-06-15T18:00:00+02:00"),
            endsAt: DateTimeOffset.Parse("2026-06-15T19:00:00+02:00"),
            status: ReservationStatus.Cancelled);
        var rule = new NoOverlappingReservationRule(host.Db);

        var result = await rule.CheckAsync(Attempt(), CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }
}
