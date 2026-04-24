using FluentAssertions;
using TennisClub.Api.Features.Reservations.Rules;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Reservations.Rules;

public class NoCourtBlockExistsRuleTests
{
    private static BookingAttempt Attempt() => new(
        Guid.NewGuid(), 1,
        DateTimeOffset.Parse("2026-06-15T18:00:00+02:00"),
        DateTimeOffset.Parse("2026-06-15T19:00:00+02:00"));

    [Fact]
    public async Task NoBlocks_Passes()
    {
        await using var host = new RuleTestHost();
        host.AddCourt();
        var rule = new NoCourtBlockExistsRule(host.Db);

        var result = await rule.CheckAsync(Attempt(), CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task OverlappingBlock_Fails()
    {
        await using var host = new RuleTestHost();
        host.AddCourt();
        host.AddCourtBlock(
            courtId: 1,
            startsAt: DateTimeOffset.Parse("2026-06-15T18:00:00+02:00"),
            endsAt: DateTimeOffset.Parse("2026-06-15T19:00:00+02:00"));
        var rule = new NoCourtBlockExistsRule(host.Db);

        var result = await rule.CheckAsync(Attempt(), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("COURT_BLOCKED");
    }

    [Fact]
    public async Task BlockOnDifferentCourt_DoesNotConflict()
    {
        await using var host = new RuleTestHost();
        host.AddCourt(1);
        host.AddCourt(2);
        host.AddCourtBlock(
            courtId: 2,
            startsAt: DateTimeOffset.Parse("2026-06-15T18:00:00+02:00"),
            endsAt: DateTimeOffset.Parse("2026-06-15T19:00:00+02:00"));
        var rule = new NoCourtBlockExistsRule(host.Db);

        var result = await rule.CheckAsync(Attempt(), CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }
}
