using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Auth.Shared;
using TennisClub.Api.Infrastructure.Auth;
using TennisClub.Api.Infrastructure.Persistence;
using TennisClub.Api.Infrastructure.Persistence.Seed;

namespace TennisClub.Api.Tests.TestInfrastructure;

/// <summary>
/// Per-test-class helper: creates a unique database inside the shared
/// Postgres container, builds an <see cref="ApiTestFactory"/>, applies
/// migrations, and exposes helpers to seed baseline data and issue JWTs.
/// </summary>
public sealed class ApiTestEnvironment : IAsyncDisposable
{
    private readonly string _masterConnectionString;
    private readonly string _dbName;
    public ApiTestFactory Factory { get; }
    public string ConnectionString { get; }

    private ApiTestEnvironment(
        string masterConnectionString,
        string dbName,
        string connectionString,
        ApiTestFactory factory)
    {
        _masterConnectionString = masterConnectionString;
        _dbName = dbName;
        ConnectionString = connectionString;
        Factory = factory;
    }

    public static async Task<ApiTestEnvironment> CreateAsync(PostgresFixture fixture)
    {
        var master = fixture.Container.GetConnectionString();
        var dbName = $"test_{Guid.NewGuid():N}";

        await using (var conn = new NpgsqlConnection(master))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE \"{dbName}\"";
            await cmd.ExecuteNonQueryAsync();
        }

        var targetConnString = new NpgsqlConnectionStringBuilder(master)
        {
            Database = dbName
        }.ConnectionString;

        var factory = new ApiTestFactory(targetConnString);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }

        return new ApiTestEnvironment(master, dbName, targetConnString, factory);
    }

    public int[] CourtIds { get; private set; } = [];

    public async Task SeedBaselineAsync(
        DateOnly? seasonStart = null,
        DateOnly? seasonEnd = null,
        int slotMinutes = 60,
        int maxOpenReservations = 10,
        int maxAdvanceDays = 30,
        int courtCount = 2)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var r in new[] { SeedData.AdminRole, SeedData.TrainerRole, SeedData.MemberRole })
        {
            if (!await roleManager.RoleExistsAsync(r))
                await roleManager.CreateAsync(new IdentityRole<Guid>(r));
        }

        var courts = new List<Court>();
        for (var i = 1; i <= courtCount; i++)
        {
            courts.Add(new Court { Name = $"Platz {i}", DisplayOrder = i, IsActive = true });
        }
        db.Courts.AddRange(courts);

        db.Seasons.Add(new Season
        {
            Name = "Test-Saison",
            StartDate = seasonStart ?? new DateOnly(2026, 1, 1),
            EndDate = seasonEnd ?? new DateOnly(2026, 12, 31),
            OpeningTime = new TimeOnly(6, 0),
            ClosingTime = new TimeOnly(23, 0),
            SlotDurationMinutes = slotMinutes
        });

        db.SystemSettings.Add(new SystemSettings
        {
            Id = 1,
            MaxAdvanceBookingDays = maxAdvanceDays,
            MinCancellationHours = 2,
            MaxOpenReservationsPerMember = maxOpenReservations
        });

        await db.SaveChangesAsync();

        CourtIds = courts.OrderBy(c => c.DisplayOrder).Select(c => c.Id).ToArray();
    }

    public async Task<Member> CreateMemberAsync(
        string email,
        string password = "Member123!",
        string[]? roles = null)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Member>>();

        var member = new Member
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = email,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(member, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ",
                result.Errors.Select(e => $"{e.Code}:{e.Description}")));

        foreach (var r in roles ?? [SeedData.MemberRole])
        {
            await userManager.AddToRoleAsync(member, r);
        }

        return member;
    }

    public string IssueJwt(Member member, IEnumerable<string>? roles = null)
    {
        using var scope = Factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<JwtTokenService>();
        return jwt.CreateAccessToken(member, roles ?? [SeedData.MemberRole]);
    }

    public HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();

        // Tear down the test database so the container stays clean across classes.
        try
        {
            await using var conn = new NpgsqlConnection(_masterConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            // Force-disconnect any leftover sessions, then drop.
            cmd.CommandText =
                $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity " +
                $"WHERE datname = '{_dbName}' AND pid <> pg_backend_pid(); " +
                $"DROP DATABASE \"{_dbName}\"";
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort cleanup; the container tears down at end-of-run anyway.
        }
    }
}
