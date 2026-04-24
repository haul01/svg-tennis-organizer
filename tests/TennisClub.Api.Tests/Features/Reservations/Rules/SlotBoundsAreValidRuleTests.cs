using FluentAssertions;
using TennisClub.Api.Features.Reservations.Rules;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Reservations.Rules;

public class SlotBoundsAreValidRuleTests
{
    private static BookingAttempt Slot(DateTimeOffset start, int minutes) =>
        new(Guid.NewGuid(), 1, start, start.AddMinutes(minutes));

    [Fact]
    public async Task CorrectDuration_Passes()
    {
        await using var host = new RuleTestHost();
        host.AddSeason(slotMinutes: 60);
        var rule = new SlotBoundsAreValidRule(host.Db, host.Time);

        var result = await rule.CheckAsync(
            Slot(DateTimeOffset.Parse("2026-05-16T18:00:00+02:00"), 60),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task WrongDuration_Fails()
    {
        await using var host = new RuleTestHost();
        host.AddSeason(slotMinutes: 60);
        var rule = new SlotBoundsAreValidRule(host.Db, host.Time);

        var result = await rule.CheckAsync(
            Slot(DateTimeOffset.Parse("2026-05-16T18:00:00+02:00"), 45),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("INVALID_DURATION");
    }

    [Fact]
    public async Task StartsAfterEnds_Fails()
    {
        await using var host = new RuleTestHost();
        host.AddSeason();
        var rule = new SlotBoundsAreValidRule(host.Db, host.Time);

        var start = DateTimeOffset.Parse("2026-05-16T19:00:00+02:00");
        var end = DateTimeOffset.Parse("2026-05-16T18:00:00+02:00");
        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1, start, end),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("INVALID_BOUNDS");
    }
}
