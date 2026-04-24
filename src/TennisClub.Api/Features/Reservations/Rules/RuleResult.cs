namespace TennisClub.Api.Features.Reservations.Rules;

public sealed record RuleResult(bool IsValid, string? Code = null, string? Message = null)
{
    public static RuleResult Ok() => new(true);
    public static RuleResult Fail(string code, string message) => new(false, code, message);
}
