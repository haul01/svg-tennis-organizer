namespace TennisClub.Api.Features.Reservations.Rules;

public sealed class SlotIsNotInPastRule(TimeProvider time) : IBookingRule
{
    public Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct) =>
        Task.FromResult(a.StartsAt > time.GetUtcNow()
            ? RuleResult.Ok()
            : RuleResult.Fail("IN_PAST", "Der Slot liegt in der Vergangenheit."));
}
