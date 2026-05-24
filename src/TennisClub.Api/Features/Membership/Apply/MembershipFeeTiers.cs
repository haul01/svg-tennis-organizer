namespace TennisClub.Api.Features.Membership.Apply;

/// <summary>
/// Mirror of the Tennis-Zweigverein fee table on the printed
/// Beitrittserklärung. The string value goes into the admin notification
/// mail so the treasurer sees exactly which tier the applicant ticked.
/// </summary>
public static class MembershipFeeTiers
{
    public const string AdultE100 = "adult";
    public const string YouthE30 = "youth";
    public const string ChildE15 = "child";
    public const string CoupleE190 = "couple";
    public const string AdultChildE100 = "adult-child";

    public static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>
        {
            [AdultE100] = "Erwachsene (€ 100,-)",
            [YouthE30] = "Jugendliche bis 18 Jahre (€ 30,-)",
            [ChildE15] = "Kinder / Schüler bis 14 Jahre (€ 15,-)",
            [CoupleE190] = "Kombi Ehepaare (€ 190,-)",
            [AdultChildE100] = "Kombi Erwachsener + Kind (€ 100,-)"
        };

    public static bool IsKnown(string? tier) =>
        tier is not null && Labels.ContainsKey(tier);

    public static string Label(string tier) =>
        Labels.TryGetValue(tier, out var label) ? label : tier;
}
