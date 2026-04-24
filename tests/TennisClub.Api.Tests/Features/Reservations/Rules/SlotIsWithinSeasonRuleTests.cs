using FluentAssertions;
using TennisClub.Api.Features.Reservations.Rules;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Reservations.Rules;

public class SlotIsWithinSeasonRuleTests
{
    [Fact]
    public async Task WithinSeason_Passes()
    {
        await using var host = new RuleTestHost();
        host.AddSeason(
            start: new DateOnly(2026, 4, 1),
            end: new DateOnly(2026, 10, 31));
        var rule = new SlotIsWithinSeasonRule(host.Db, host.Time);

        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1,
                DateTimeOffset.Parse("2026-06-15T18:00:00+02:00"),
                DateTimeOffset.Parse("2026-06-15T19:00:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NoActiveSeason_Fails()
    {
        await using var host = new RuleTestHost(DateTimeOffset.Parse("2026-02-01T10:00:00+01:00"));
        // Season starts April → no active season on Feb 1.
        host.AddSeason(
            start: new DateOnly(2026, 4, 1),
            end: new DateOnly(2026, 10, 31));
        var rule = new SlotIsWithinSeasonRule(host.Db, host.Time);

        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1,
                DateTimeOffset.Parse("2026-02-15T18:00:00+01:00"),
                DateTimeOffset.Parse("2026-02-15T19:00:00+01:00")),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("NO_SEASON");
    }

    [Fact]
    public async Task SlotOutsideSeasonWindow_Fails()
    {
        await using var host = new RuleTestHost();
        host.AddSeason(
            start: new DateOnly(2026, 4, 1),
            end: new DateOnly(2026, 10, 31));
        var rule = new SlotIsWithinSeasonRule(host.Db, host.Time);

        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1,
                DateTimeOffset.Parse("2026-11-15T18:00:00+01:00"),
                DateTimeOffset.Parse("2026-11-15T19:00:00+01:00")),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("OUTSIDE_SEASON");
    }
}
