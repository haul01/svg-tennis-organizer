using FluentAssertions;
using TennisClub.Api.Features.Reservations.Rules;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Reservations.Rules;

public class CourtIsActiveRuleTests
{
    private static BookingAttempt Attempt(int courtId) => new(
        Guid.NewGuid(), courtId,
        DateTimeOffset.Parse("2026-06-15T18:00:00+02:00"),
        DateTimeOffset.Parse("2026-06-15T19:00:00+02:00"));

    [Fact]
    public async Task ActiveCourt_Passes()
    {
        await using var host = new RuleTestHost();
        host.AddCourt(1, "Platz 1", active: true);
        var rule = new CourtIsActiveRule(host.Db);

        var result = await rule.CheckAsync(Attempt(1), CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task InactiveCourt_Fails()
    {
        await using var host = new RuleTestHost();
        host.AddCourt(1, "Platz 1", active: false);
        var rule = new CourtIsActiveRule(host.Db);

        var result = await rule.CheckAsync(Attempt(1), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("COURT_INACTIVE");
    }

    [Fact]
    public async Task UnknownCourt_Fails()
    {
        await using var host = new RuleTestHost();
        var rule = new CourtIsActiveRule(host.Db);

        var result = await rule.CheckAsync(Attempt(999), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("COURT_UNKNOWN");
    }
}
