using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Domain.Enums;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Tests.TestInfrastructure;

/// <summary>
/// Lightweight DbContext-on-SQLite-in-memory host for booking-rule unit tests.
/// Each instance has its own DB; disposal closes the connection which drops it.
/// </summary>
public sealed class RuleTestHost : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    public AppDbContext Db { get; }
    public FakeTimeProvider Time { get; }

    public RuleTestHost(DateTimeOffset? now = null)
    {
        Time = new FakeTimeProvider(now ?? DateTimeOffset.Parse("2026-05-15T10:00:00+02:00"));

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Db = new TestAppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).Options);
        Db.Database.EnsureCreated();
    }

    public Court AddCourt(int id = 1, string name = "Platz 1", bool active = true)
    {
        var c = new Court { Id = id, Name = name, DisplayOrder = id, IsActive = active };
        Db.Courts.Add(c);
        Db.SaveChanges();
        return c;
    }

    public Season AddSeason(
        DateOnly? start = null,
        DateOnly? end = null,
        TimeOnly? openingTime = null,
        TimeOnly? closingTime = null,
        int slotMinutes = 60)
    {
        var s = new Season
        {
            Name = "Saison",
            StartDate = start ?? new DateOnly(2026, 4, 1),
            EndDate = end ?? new DateOnly(2026, 10, 31),
            OpeningTime = openingTime ?? new TimeOnly(8, 0),
            ClosingTime = closingTime ?? new TimeOnly(22, 0),
            SlotDurationMinutes = slotMinutes
        };
        Db.Seasons.Add(s);
        Db.SaveChanges();
        return s;
    }

    public SystemSettings AddSystemSettings(
        int maxAdvanceDays = 7,
        int minCancelHours = 2,
        int maxOpen = 2)
    {
        var s = new SystemSettings
        {
            Id = 1,
            MaxAdvanceBookingDays = maxAdvanceDays,
            MinCancellationHours = minCancelHours,
            MaxOpenReservationsPerMember = maxOpen
        };
        Db.SystemSettings.Add(s);
        Db.SaveChanges();
        return s;
    }

    public Member AddMember(Guid? id = null)
    {
        var m = new Member
        {
            Id = id ?? Guid.NewGuid(),
            UserName = "member",
            Email = "member@test",
            FirstName = "Test",
            LastName = "Member",
            IsActive = true,
            CreatedAt = Time.GetUtcNow()
        };
        Db.Users.Add(m);
        Db.SaveChanges();
        return m;
    }

    public Reservation AddReservation(
        int courtId,
        Guid memberId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        ReservationStatus status = ReservationStatus.Active)
    {
        var r = new Reservation
        {
            Id = Guid.NewGuid(),
            CourtId = courtId,
            MemberId = memberId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Status = status,
            CreatedAt = Time.GetUtcNow(),
            // SQLite has no server-side rowversion generation; provide a value
            // so the NOT NULL constraint on byte[] holds.
            RowVersion = [0]
        };
        Db.Reservations.Add(r);
        Db.SaveChanges();
        return r;
    }

    public CourtBlock AddCourtBlock(
        int courtId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string reason = "Training")
    {
        var b = new CourtBlock
        {
            Id = Guid.NewGuid(),
            CourtId = courtId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Reason = reason,
            CreatedAt = Time.GetUtcNow(),
            CreatedByMemberId = Guid.NewGuid()
        };
        Db.CourtBlocks.Add(b);
        Db.SaveChanges();
        return b;
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
