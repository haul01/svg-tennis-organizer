using FluentAssertions;
using TennisClub.Api.Features.Reservations.Rules;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Reservations.Rules;

public class SlotIsNotInPastRuleTests
{
    [Fact]
    public async Task SlotFullyInPast_Fails()
    {
        // Now is 18:00, slot was 10:00-11:00 → ended 7 hours ago.
        await using var host = new RuleTestHost(DateTimeOffset.Parse("2026-05-15T18:00:00+02:00"));
        var rule = new SlotIsNotInPastRule(host.Time);

        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1,
                DateTimeOffset.Parse("2026-05-15T10:00:00+02:00"),
                DateTimeOffset.Parse("2026-05-15T11:00:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("IN_PAST");
    }

    [Fact]
    public async Task FutureSlot_Passes()
    {
        await using var host = new RuleTestHost(DateTimeOffset.Parse("2026-05-15T10:00:00+02:00"));
        var rule = new SlotIsNotInPastRule(host.Time);

        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1,
                DateTimeOffset.Parse("2026-05-16T18:00:00+02:00"),
                DateTimeOffset.Parse("2026-05-16T19:00:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SlotAlreadyStartedButNotYetEnded_Passes()
    {
        // Now is 12:15, slot is 12:00-12:30 → started, still running.
        // Matches what the week grid shows: still 'free' until 12:30.
        await using var host = new RuleTestHost(DateTimeOffset.Parse("2026-05-15T12:15:00+02:00"));
        var rule = new SlotIsNotInPastRule(host.Time);

        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1,
                DateTimeOffset.Parse("2026-05-15T12:00:00+02:00"),
                DateTimeOffset.Parse("2026-05-15T12:30:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }
}
