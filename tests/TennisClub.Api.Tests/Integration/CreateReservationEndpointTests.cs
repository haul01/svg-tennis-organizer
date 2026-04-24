using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Features.Reservations.Create;
using TennisClub.Api.Infrastructure.Persistence;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Integration;

[Collection("Sql")]
public class CreateReservationEndpointTests(MsSqlFixture sql) : IAsyncLifetime
{
    private ApiTestEnvironment _env = null!;

    public async Task InitializeAsync()
    {
        _env = await ApiTestEnvironment.CreateAsync(sql);
        await _env.SeedBaselineAsync();
    }

    public async Task DisposeAsync() => await _env.DisposeAsync();

    [Fact]
    public async Task Create_ValidRequest_Returns201AndPersistsReservation()
    {
        var member = await _env.CreateMemberAsync("alice@club.test");
        var token = _env.IssueJwt(member);
        var client = _env.CreateAuthenticatedClient(token);

        var startsAt = NextFutureSlot();
        var request = new CreateReservationRequest(
            CourtId: _env.CourtIds[0],
            StartsAt: startsAt,
            EndsAt: startsAt.AddHours(1),
            GuestPlayerId: null);

        var response = await client.PostAsJsonAsync("/api/reservations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _env.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.Reservations.SingleAsync();
        stored.MemberId.Should().Be(member.Id);
        stored.Status.Should().Be(ReservationStatus.Active);
    }

    [Fact]
    public async Task Create_ParallelRequestsOnSameSlot_ExactlyOneSucceeds()
    {
        // Two distinct members both try to book the same court+slot at the same
        // time. The filtered unique index on (CourtId, StartsAt) WHERE Status=0
        // must allow only one row through; the other request has to come back
        // as 409 Conflict via the DbUpdateException catch in the handler.
        var alice = await _env.CreateMemberAsync("alice@club.test");
        var bob = await _env.CreateMemberAsync("bob@club.test");

        var clientA = _env.CreateAuthenticatedClient(_env.IssueJwt(alice));
        var clientB = _env.CreateAuthenticatedClient(_env.IssueJwt(bob));

        var startsAt = NextFutureSlot();
        var request = new CreateReservationRequest(
            CourtId: _env.CourtIds[0],
            StartsAt: startsAt,
            EndsAt: startsAt.AddHours(1),
            GuestPlayerId: null);

        var taskA = Task.Run(() => clientA.PostAsJsonAsync("/api/reservations", request));
        var taskB = Task.Run(() => clientB.PostAsJsonAsync("/api/reservations", request));
        var responses = await Task.WhenAll(taskA, taskB);

        var statusCodes = responses.Select(r => r.StatusCode).OrderBy(c => c).ToArray();
        statusCodes.Should().Equal(HttpStatusCode.Created, HttpStatusCode.Conflict);

        using var scope = _env.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Reservations
            .Where(r => r.Status == ReservationStatus.Active)
            .CountAsync();
        count.Should().Be(1, "exactly one active reservation per (CourtId, StartsAt) slot is allowed");
    }

    [Fact]
    public async Task Create_SlotInPast_ReturnsBadRequestWithRuleFailure()
    {
        var member = await _env.CreateMemberAsync("dave@club.test");
        var client = _env.CreateAuthenticatedClient(_env.IssueJwt(member));

        var past = DateTimeOffset.UtcNow.AddDays(-2);
        var request = new CreateReservationRequest(
            CourtId: _env.CourtIds[0],
            StartsAt: past,
            EndsAt: past.AddHours(1),
            GuestPlayerId: null);

        var response = await client.PostAsJsonAsync("/api/reservations", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("IN_PAST");
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        var client = _env.Factory.CreateClient();
        var startsAt = NextFutureSlot();
        var request = new CreateReservationRequest(
            _env.CourtIds[0], startsAt, startsAt.AddHours(1), null);

        var response = await client.PostAsJsonAsync("/api/reservations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Aligns to the next full hour at least 2 days out, keeping clear of
    // past-slot and within-season rule boundaries regardless of run time.
    private static DateTimeOffset NextFutureSlot()
    {
        var now = DateTimeOffset.UtcNow;
        var target = now.AddDays(2);
        return new DateTimeOffset(
            target.Year, target.Month, target.Day, 10, 0, 0, TimeSpan.Zero);
    }
}
