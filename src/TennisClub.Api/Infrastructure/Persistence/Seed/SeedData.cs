using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TennisClub.Api.Domain.Entities;

namespace TennisClub.Api.Infrastructure.Persistence.Seed;

public static class SeedData
{
    public const string AdminRole = "Admin";
    public const string TrainerRole = "Trainer";
    public const string MemberRole = "Member";

    public static async Task RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<Member>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var time = sp.GetRequiredService<TimeProvider>();
        var options = sp.GetRequiredService<IOptions<SeedOptions>>().Value;

        await EnsureRolesAsync(roleManager);
        await EnsureAdminAsync(userManager, options.Admin, time, ct);
        await EnsureCourtsAsync(db, options.Courts, ct);
        await EnsureSeasonAsync(db, options.Season, ct);
        await EnsureSystemSettingsAsync(db, ct);
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in new[] { AdminRole, TrainerRole, MemberRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }

    private static async Task EnsureAdminAsync(
        UserManager<Member> userManager,
        SeedOptions.AdminOptions admin,
        TimeProvider time,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password))
        {
            throw new InvalidOperationException(
                "Seed:Admin:Email and Seed:Admin:Password must be configured.");
        }

        var existing = await userManager.FindByEmailAsync(admin.Email);
        if (existing is not null) return;

        var user = new Member
        {
            Id = Guid.NewGuid(),
            UserName = admin.Email,
            Email = admin.Email,
            EmailConfirmed = true,
            FirstName = admin.FirstName,
            LastName = admin.LastName,
            IsActive = true,
            CreatedAt = time.GetUtcNow()
        };

        var result = await userManager.CreateAsync(user, admin.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"Seed admin creation failed: {errors}");
        }

        await userManager.AddToRoleAsync(user, AdminRole);
    }

    private static async Task EnsureCourtsAsync(
        AppDbContext db,
        IReadOnlyCollection<SeedOptions.CourtSeedOptions> courts,
        CancellationToken ct)
    {
        if (await db.Courts.AnyAsync(ct)) return;
        if (courts.Count == 0) return;

        foreach (var c in courts)
        {
            db.Courts.Add(new Court
            {
                Name = c.Name,
                DisplayOrder = c.DisplayOrder,
                IsActive = true
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureSeasonAsync(
        AppDbContext db,
        SeedOptions.SeasonOptions season,
        CancellationToken ct)
    {
        if (await db.Seasons.AnyAsync(ct)) return;
        if (string.IsNullOrWhiteSpace(season.Name)) return;

        db.Seasons.Add(new Season
        {
            Name = season.Name,
            StartDate = season.StartDate,
            EndDate = season.EndDate,
            OpeningTime = season.OpeningTime,
            ClosingTime = season.ClosingTime,
            SlotDurationMinutes = season.SlotDurationMinutes
        });
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureSystemSettingsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.SystemSettings.AnyAsync(ct)) return;

        db.SystemSettings.Add(new SystemSettings
        {
            Id = 1,
            MaxAdvanceBookingDays = 7,
            MinCancellationHours = 0,
            MaxOpenReservationsPerMember = 2
        });
        await db.SaveChangesAsync(ct);
    }
}
