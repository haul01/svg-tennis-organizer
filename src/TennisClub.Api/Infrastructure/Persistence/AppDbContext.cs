using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TennisClub.Api.Domain.Entities;

namespace TennisClub.Api.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<Member, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<GuestPlayer> GuestPlayers => Set<GuestPlayer>();
    public DbSet<CourtBlock> CourtBlocks => Set<CourtBlock>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
