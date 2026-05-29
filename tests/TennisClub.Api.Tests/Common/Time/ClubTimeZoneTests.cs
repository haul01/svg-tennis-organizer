using FluentAssertions;
using TennisClub.Api.Common.Time;

namespace TennisClub.Api.Tests.Common.Time;

public class ClubTimeZoneTests
{
    [Fact]
    public void ToInstant_SummerWallClock_UsesCestOffsetNotUtc()
    {
        // 1 July 18:00 in Vienna is CEST (+02:00), i.e. 16:00Z - NOT 18:00Z.
        var instant = ClubTimeZone.ToInstant(new DateTime(2026, 7, 1, 18, 0, 0));

        instant.Offset.Should().Be(TimeSpan.FromHours(2));
        instant.UtcDateTime.Should().Be(new DateTime(2026, 7, 1, 16, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ToInstant_WinterWallClock_UsesCetOffsetNotUtc()
    {
        // 1 December 18:00 in Vienna is CET (+01:00), i.e. 17:00Z.
        var instant = ClubTimeZone.ToInstant(new DateTime(2026, 12, 1, 18, 0, 0));

        instant.Offset.Should().Be(TimeSpan.FromHours(1));
        instant.UtcDateTime.Should().Be(new DateTime(2026, 12, 1, 17, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ToInstant_RoundTripsBackToTheSameLocalWallClock()
    {
        var wall = new DateTime(2026, 7, 1, 18, 0, 0);

        var instant = ClubTimeZone.ToInstant(wall);

        ClubTimeZone.LocalDateTime(instant).Should().Be(wall);
    }
}
