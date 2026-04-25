namespace TennisClub.Api.Common.Time;

/// <summary>
/// Single-tenant convenience: the club lives in Austria (Europe/Vienna),
/// so every wall-clock comparison (opening hours, season date, ...) needs
/// to be done against this zone, regardless of how the client serialises
/// timestamps. JS `Date.toISOString()` always emits UTC, so we cannot
/// trust DateTimeOffset.DateTime to give us the booking's local time.
/// </summary>
public static class ClubTimeZone
{
    public static readonly TimeZoneInfo Vienna = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "W. Europe Standard Time" : "Europe/Vienna");

    /// <summary>Returns the wall-clock DateTime of <paramref name="value"/> in Vienna.</summary>
    public static DateTime LocalDateTime(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, Vienna).DateTime;

    public static DateOnly LocalDate(DateTimeOffset value) =>
        DateOnly.FromDateTime(LocalDateTime(value));

    public static TimeOnly LocalTimeOfDay(DateTimeOffset value) =>
        TimeOnly.FromDateTime(LocalDateTime(value));
}
