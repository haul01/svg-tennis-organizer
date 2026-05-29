using FluentAssertions;
using TennisClub.Api.Common.Time;
using TennisClub.Api.Features.CourtBlocks.CreateSeries;

namespace TennisClub.Api.Tests.Features.CourtBlocks;

/// <summary>
/// Regression cover for the bug where weekly series blocks stored the
/// admin's Austrian wall-clock time as UTC (TimeSpan.Zero), shifting every
/// block 1-2h off the intended slot and drifting across DST.
/// </summary>
public class CreateSeriesExpansionTests
{
    [Fact]
    public void ExpandSeries_StoresAustrianWallClockAsLocalInstant_NotUtc()
    {
        // Mondays 18:00-19:00 local across two summer weeks (CEST, +02:00).
        var req = new CreateSeriesRequest(
            CourtId: 1,
            Weekday: DayOfWeek.Monday,
            StartTime: new TimeOnly(18, 0),
            EndTime: new TimeOnly(19, 0),
            StartDate: new DateOnly(2026, 7, 6),   // a Monday
            EndDate: new DateOnly(2026, 7, 13),    // the next Monday
            Reason: "Training",
            ForceCancelConflicts: false);

        var intervals = CreateSeriesHandler.ExpandSeries(req, [1]);

        intervals.Should().HaveCount(2);
        foreach (var i in intervals)
        {
            // Vienna 18:00 == 16:00Z in summer. The old code wrote 18:00Z.
            i.StartsAt.Offset.Should().Be(TimeSpan.FromHours(2));
            i.StartsAt.UtcDateTime.TimeOfDay.Should().Be(TimeSpan.FromHours(16));
            ClubTimeZone.LocalTimeOfDay(i.StartsAt).Should().Be(new TimeOnly(18, 0));
            ClubTimeZone.LocalTimeOfDay(i.EndsAt).Should().Be(new TimeOnly(19, 0));
        }
    }

    [Fact]
    public void ExpandSeries_AcrossDstBoundary_KeepsTheSameLocalTime()
    {
        // DST ends Sun 2026-10-25. Mon 19 Oct is CEST (+02:00), Mon 26 Oct is
        // CET (+01:00); both must still represent local 18:00.
        var req = new CreateSeriesRequest(
            CourtId: 1,
            Weekday: DayOfWeek.Monday,
            StartTime: new TimeOnly(18, 0),
            EndTime: new TimeOnly(19, 0),
            StartDate: new DateOnly(2026, 10, 19),
            EndDate: new DateOnly(2026, 10, 26),
            Reason: "Training",
            ForceCancelConflicts: false);

        var intervals = CreateSeriesHandler.ExpandSeries(req, [1]);

        intervals.Should().HaveCount(2);
        intervals[0].StartsAt.Offset.Should().Be(TimeSpan.FromHours(2)); // 19 Oct, CEST
        intervals[1].StartsAt.Offset.Should().Be(TimeSpan.FromHours(1)); // 26 Oct, CET
        intervals.Should().AllSatisfy(i =>
            ClubTimeZone.LocalTimeOfDay(i.StartsAt).Should().Be(new TimeOnly(18, 0)));
    }
}
