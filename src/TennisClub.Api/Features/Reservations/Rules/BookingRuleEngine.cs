namespace TennisClub.Api.Features.Reservations.Rules;

/// <summary>
/// Runs all registered rules against a booking attempt and collects every
/// failure. No fail-fast: the caller should show the user all problems at once.
/// </summary>
public sealed class BookingRuleEngine(IEnumerable<IBookingRule> rules)
{
    public async Task<IReadOnlyList<RuleResult>> CheckAsync(
        BookingAttempt attempt, CancellationToken ct)
    {
        var failures = new List<RuleResult>();
        foreach (var rule in rules)
        {
            var result = await rule.CheckAsync(attempt, ct);
            if (!result.IsValid) failures.Add(result);
        }
        return failures;
    }
}
