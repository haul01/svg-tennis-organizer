using FluentAssertions;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Features.Reservations.Cancel;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Reservations;

public class CancelReservationHandlerTests
{
    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-06-15T10:00:00+02:00");

    private static RuleTestHost NewHost()
    {
        var host = new RuleTestHost(Now);
        host.AddCourt();
        host.AddSystemSettings(minCancelHours: 2);
        return host;
    }

    [Fact]
    public async Task Cancel_HappyPath_SetsStatusToCancelled()
    {
        await using var host = NewHost();
        var member = host.AddMember();
        var r = host.AddReservation(1, member.Id,
            Now.AddDays(1), Now.AddDays(1).AddHours(1));
        var handler = new CancelReservationHandler(host.Db, host.Email, host.Templates, host.Time);

        var result = await handler.HandleAsync(r.Id, member.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        host.Db.ChangeTracker.Clear();
        var reloaded = await host.Db.Reservations.FindAsync(r.Id);
        reloaded!.Status.Should().Be(ReservationStatus.Cancelled);
        reloaded.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Cancel_NotOwnedByMember_ReturnsNotFound()
    {
        await using var host = NewHost();
        var owner = host.AddMember();
        var intruder = host.AddMember();
        var r = host.AddReservation(1, owner.Id,
            Now.AddDays(1), Now.AddDays(1).AddHours(1));
        var handler = new CancelReservationHandler(host.Db, host.Email, host.Templates, host.Time);

        var result = await handler.HandleAsync(r.Id, intruder.Id, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Cancel_UnknownId_ReturnsNotFound()
    {
        await using var host = NewHost();
        var member = host.AddMember();
        var handler = new CancelReservationHandler(host.Db, host.Email, host.Templates, host.Time);

        var result = await handler.HandleAsync(
            Guid.NewGuid(), member.Id, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_ReturnsInvalid()
    {
        await using var host = NewHost();
        var member = host.AddMember();
        var r = host.AddReservation(1, member.Id,
            Now.AddDays(1), Now.AddDays(1).AddHours(1),
            status: ReservationStatus.Cancelled);
        var handler = new CancelReservationHandler(host.Db, host.Email, host.Templates, host.Time);

        var result = await handler.HandleAsync(r.Id, member.Id, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
        result.Error.Should().Contain("bereits storniert");
    }

    [Fact]
    public async Task Cancel_WithinMinCancellationWindow_ReturnsInvalid()
    {
        await using var host = NewHost();
        var member = host.AddMember();
        // 30 minutes from now, inside the 2h window.
        var r = host.AddReservation(1, member.Id,
            Now.AddMinutes(30), Now.AddMinutes(90));
        var handler = new CancelReservationHandler(host.Db, host.Email, host.Templates, host.Time);

        var result = await handler.HandleAsync(r.Id, member.Id, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
        result.Error.Should().Contain("2 Stunden vor Beginn");

        host.Db.ChangeTracker.Clear();
        var reloaded = await host.Db.Reservations.FindAsync(r.Id);
        reloaded!.Status.Should().Be(ReservationStatus.Active, "late cancel must not mutate state");
    }
}
