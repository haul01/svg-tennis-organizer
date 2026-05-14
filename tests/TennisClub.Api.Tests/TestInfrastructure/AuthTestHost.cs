using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Auth.ForgotPassword;
using TennisClub.Api.Features.Auth.Login;
using TennisClub.Api.Features.Auth.Refresh;
using TennisClub.Api.Features.Auth.ResetPassword;
using TennisClub.Api.Features.Auth.Shared;
using TennisClub.Api.Infrastructure.Auth;
using TennisClub.Api.Infrastructure.Email;
using TennisClub.Api.Infrastructure.Persistence;
using TennisClub.Api.Infrastructure.Persistence.Seed;

namespace TennisClub.Api.Tests.TestInfrastructure;

/// <summary>
/// Spins up an in-memory SQLite-backed DI container with just enough of the
/// application's auth stack to exercise handlers end-to-end without HTTP.
/// Each test class instantiates one host; tests share the same DB within.
/// </summary>
public sealed class AuthTestHost : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    public ServiceProvider Services { get; }
    public FakeTimeProvider Time { get; }
    public JwtSettings JwtSettings { get; }
    public CollectingEmailSender Emails { get; } = new();

    public AuthTestHost(DateTimeOffset? now = null)
    {
        Time = new FakeTimeProvider(now ?? DateTimeOffset.Parse("2026-04-23T10:00:00Z"));

        JwtSettings = new JwtSettings
        {
            Issuer = "TennisClub.Api.Tests",
            Audience = "TennisClub.Api.Tests",
            SigningKey = "test-signing-key-minimum-32-characters-long!",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        };

        // In-memory SQLite that stays alive as long as the connection is open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(_connection));
        services.AddLogging(b => b.AddFilter(_ => false));
        services.AddDataProtection();

        services.AddIdentityCore<Member>(opts =>
            {
                opts.Password.RequiredLength = 8;
                opts.User.RequireUniqueEmail = true;
                opts.SignIn.RequireConfirmedEmail = false;
                opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                opts.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<TimeProvider>(Time);
        services.AddSingleton(JwtSettings);
        services.AddScoped<JwtTokenService>();

        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshHandler>();
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddSingleton<IEmailSender>(Emails);
        services.AddSingleton<EmailQueue>();
        services.AddSingleton<EmailTemplateRenderer>();
        services.Configure<FrontendSettings>(o => o.BaseUrl = "http://localhost:4200");

        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    /// <summary>
    /// Pre-creates the four production roles (Admin, Trainer, Member, Guest)
    /// so handlers that switch roles can find them. Idempotent.
    /// </summary>
    public async Task EnsureAllRolesAsync()
    {
        using var scope = Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var r in new[]
        {
            SeedData.AdminRole, SeedData.TrainerRole,
            SeedData.MemberRole, SeedData.GuestRole
        })
        {
            if (!await roleManager.RoleExistsAsync(r))
                await roleManager.CreateAsync(new IdentityRole<Guid>(r));
        }
    }

    public async Task<Member> SeedMemberAsync(
        string email = "member@tennisclub.local",
        string password = "Member123!",
        bool isActive = true,
        string[]? roles = null)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Member>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var r in roles ?? [SeedData.MemberRole])
        {
            if (!await roleManager.RoleExistsAsync(r))
                await roleManager.CreateAsync(new IdentityRole<Guid>(r));
        }

        var user = new Member
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User",
            IsActive = isActive,
            CreatedAt = Time.GetUtcNow()
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Failed to seed test member: " +
                string.Join("; ", result.Errors.Select(e => $"{e.Code}:{e.Description}")));

        foreach (var r in roles ?? [SeedData.MemberRole])
        {
            await userManager.AddToRoleAsync(user, r);
        }

        return user;
    }

    public async Task<RefreshToken> SeedRefreshTokenAsync(
        Guid memberId,
        string rawToken,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? revokedAt = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            TokenHash = JwtTokenService.Hash(rawToken),
            ExpiresAt = expiresAt ?? Time.GetUtcNow().AddDays(7),
            CreatedAt = Time.GetUtcNow(),
            RevokedAt = revokedAt
        };
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();
        return token;
    }

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
