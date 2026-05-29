using FluentAssertions;
using TennisClub.Api.Features.Reservations.Rules;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Reservations.Rules;

public class SlotIsWithinOpeningHoursRuleTests
{
    [Fact]
    public async Task WithinHours_Passes()
    {
        await using var host = new RuleTestHost();
        host.AddSeason(openingTime: new TimeOnly(8, 0), closingTime: new TimeOnly(22, 0));
        var rule = new SlotIsWithinOpeningHoursRule(host.Db, host.Time);

        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1,
                DateTimeOffset.Parse("2026-06-15T18:00:00+02:00"),
                DateTimeOffset.Parse("2026-06-15T19:00:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BeforeOpening_Fails()
    {
        await using var host = new RuleTestHost();
        host.AddSeason(openingTime: new TimeOnly(8, 0), closingTime: new TimeOnly(22, 0));
        var rule = new SlotIsWithinOpeningHoursRule(host.Db, host.Time);

        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1,
                DateTimeOffset.Parse("2026-06-15T07:00:00+02:00"),
                DateTimeOffset.Parse("2026-06-15T08:00:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("OUTSIDE_HOURS");
    }

    [Fact]
    public async Task AfterClosing_Fails()
    {
        await using var host = new RuleTestHost();
        host.AddSeason(openingTime: new TimeOnly(8, 0), closingTime: new TimeOnly(22, 0));
        var rule = new SlotIsWithinOpeningHoursRule(host.Db, host.Time);

        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1,
                DateTimeOffset.Parse("2026-06-15T22:00:00+02:00"),
                DateTimeOffset.Parse("2026-06-15T23:00:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("OUTSIDE_HOURS");
    }

    [Fact]
    public async Task EndingPastMidnight_Fails()
    {
        await using var host = new RuleTestHost();
        host.AddSeason(openingTime: new TimeOnly(8, 0), closingTime: new TimeOnly(22, 0));
        var rule = new SlotIsWithinOpeningHoursRule(host.Db, host.Time);

        // A multi-slot booking 21:00 -> 01:00 next day. The end's time-of-day
        // (01:00) is below ClosingTime, which is exactly what let it slip
        // through before the fix; it must now be rejected.
        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1,
                DateTimeOffset.Parse("2026-06-15T21:00:00+02:00"),
                DateTimeOffset.Parse("2026-06-16T01:00:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("OUTSIDE_HOURS");
    }

    [Fact]
    public async Task EndingExactlyAtClosing_Passes()
    {
        await using var host = new RuleTestHost();
        host.AddSeason(openingTime: new TimeOnly(8, 0), closingTime: new TimeOnly(22, 0));
        var rule = new SlotIsWithinOpeningHoursRule(host.Db, host.Time);

        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1,
                DateTimeOffset.Parse("2026-06-15T21:00:00+02:00"),
                DateTimeOffset.Parse("2026-06-15T22:00:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NoActiveSeason_StaysSilent()
    {
        // SlotIsWithinSeasonRule owns the NO_SEASON failure; this rule must not duplicate it.
        await using var host = new RuleTestHost(DateTimeOffset.Parse("2026-02-01T10:00:00+01:00"));
        host.AddSeason(
            start: new DateOnly(2026, 4, 1),
            end: new DateOnly(2026, 10, 31));
        var rule = new SlotIsWithinOpeningHoursRule(host.Db, host.Time);

        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1,
                DateTimeOffset.Parse("2026-02-05T05:00:00+01:00"),
                DateTimeOffset.Parse("2026-02-05T06:00:00+01:00")),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }
}
