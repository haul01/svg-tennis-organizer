using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Tests.TestInfrastructure;

/// <summary>
/// Test-only subclass of <see cref="AppDbContext"/> that neutralises
/// SQL-Server-specific mapping details so SQLite-backed unit tests work:
///   - rowversion byte[] with a default value (SQLite has no server-side
///     rowversion generation)
///   - DateTimeOffset via binary conversion so range queries translate.
/// Real concurrency and timezone behaviour is covered by SQL-Server
/// integration tests in Phase 3.
/// </summary>
public sealed class TestAppDbContext(DbContextOptions<AppDbContext> options)
    : AppDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var dtoConverter = new DateTimeOffsetToBinaryConverter();
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset)
                    || property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(dtoConverter);
                }
            }
        }

        builder.Entity<Reservation>()
            .Property(r => r.RowVersion)
            .ValueGeneratedNever()
            .HasDefaultValue(new byte[] { 0 });
    }
}
