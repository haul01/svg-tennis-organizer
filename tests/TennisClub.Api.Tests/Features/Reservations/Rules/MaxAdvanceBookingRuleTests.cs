using FluentAssertions;
using TennisClub.Api.Features.Reservations.Rules;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Reservations.Rules;

public class MaxAdvanceBookingRuleTests
{
    [Fact]
    public async Task WithinLimit_Passes()
    {
        await using var host = new RuleTestHost(DateTimeOffset.Parse("2026-06-15T10:00:00+02:00"));
        host.AddSystemSettings(maxAdvanceDays: 7);
        var rule = new MaxAdvanceBookingRule(host.Db, host.Time);

        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1,
                DateTimeOffset.Parse("2026-06-20T18:00:00+02:00"),
                DateTimeOffset.Parse("2026-06-20T19:00:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BeyondLimit_Fails()
    {
        await using var host = new RuleTestHost(DateTimeOffset.Parse("2026-06-15T10:00:00+02:00"));
        host.AddSystemSettings(maxAdvanceDays: 7);
        var rule = new MaxAdvanceBookingRule(host.Db, host.Time);

        var result = await rule.CheckAsync(
            new BookingAttempt(Guid.NewGuid(), 1,
                DateTimeOffset.Parse("2026-06-25T18:00:00+02:00"),
                DateTimeOffset.Parse("2026-06-25T19:00:00+02:00")),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("TOO_FAR");
    }
}
