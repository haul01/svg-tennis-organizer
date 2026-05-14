using FluentAssertions;
using TennisClub.Api.Features.Reservations.Rules;
using TennisClub.Api.Infrastructure.Persistence.Seed;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Reservations.Rules;

public class CourtAllowsGuestRuleTests
{
    private static BookingAttempt Attempt(int courtId, params string[] roles) => new(
        Guid.NewGuid(),
        courtId,
        DateTimeOffset.Parse("2026-06-15T18:00:00+02:00"),
        DateTimeOffset.Parse("2026-06-15T19:00:00+02:00"),
        roles);

    [Fact]
    public async Task Member_OnGuestRestrictedCourt_Passes()
    {
        await using var host = new RuleTestHost();
        host.AddCourt(1, "Platz 1"); // IsGuestBookable default false
        var rule = new CourtAllowsGuestRule(host.Db);

        var result = await rule.CheckAsync(
            Attempt(1, SeedData.MemberRole), CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Guest_OnRestrictedCourt_Fails()
    {
        await using var host = new RuleTestHost();
        host.AddCourt(1, "Platz 1"); // IsGuestBookable false
        var rule = new CourtAllowsGuestRule(host.Db);

        var result = await rule.CheckAsync(
            Attempt(1, SeedData.GuestRole), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Code.Should().Be("GUEST_COURT_NOT_ALLOWED");
        result.Message.Should().Contain("nicht freigegeben");
    }

    [Fact]
    public async Task Guest_OnAllowedCourt_Passes()
    {
        await using var host = new RuleTestHost();
        host.AddCourt(3, "Platz 3", guestBookable: true);
        var rule = new CourtAllowsGuestRule(host.Db);

        var result = await rule.CheckAsync(
            Attempt(3, SeedData.GuestRole), CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NoRoles_BypassesRule()
    {
        // Defensive: an attempt with empty roles (e.g. a not-yet-loaded
        // user or a test path) should not block. CourtIsActiveRule would
        // catch a non-existent / inactive court anyway.
        await using var host = new RuleTestHost();
        host.AddCourt(1, "Platz 1");
        var rule = new CourtAllowsGuestRule(host.Db);

        var result = await rule.CheckAsync(Attempt(1), CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UnknownCourt_BypassesRule()
    {
        // CourtIsActiveRule reports COURT_UNKNOWN; this rule doesn't
        // duplicate it. We just don't fail with GUEST_COURT_NOT_ALLOWED
        // for a court that doesn't exist.
        await using var host = new RuleTestHost();
        var rule = new CourtAllowsGuestRule(host.Db);

        var result = await rule.CheckAsync(
            Attempt(999, SeedData.GuestRole), CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }
}
