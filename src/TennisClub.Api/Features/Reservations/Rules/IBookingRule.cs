namespace TennisClub.Api.Features.Reservations.Rules;

public interface IBookingRule
{
    Task<RuleResult> CheckAsync(BookingAttempt attempt, CancellationToken ct);
}
