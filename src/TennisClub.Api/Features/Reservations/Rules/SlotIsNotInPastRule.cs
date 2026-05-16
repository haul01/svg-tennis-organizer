namespace TennisClub.Api.Features.Reservations.Rules;

public sealed class SlotIsNotInPastRule(TimeProvider time) : IBookingRule
{
    // A slot counts as "past" only once it's fully over. A slot whose
    // start time has slipped past but whose end time is still in the
    // future remains bookable - matches what the week grid displays
    // as "free" (cells flip to past only at rowEnd <= now). Without
    // this, the grid offered a click that the backend then rejected.
    public Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct) =>
        Task.FromResult(a.EndsAt > time.GetUtcNow()
            ? RuleResult.Ok()
            : RuleResult.Fail("IN_PAST", "Der Slot liegt in der Vergangenheit."));
}
