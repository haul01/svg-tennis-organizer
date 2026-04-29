using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TennisClub.Api.Infrastructure.Persistence;

namespace TennisClub.Api.Tests.TestInfrastructure;

/// <summary>
/// Test-only subclass of <see cref="AppDbContext"/> that neutralises
/// provider-specific mapping details so the SQLite-backed unit tests
/// translate cleanly: DateTimeOffset → binary so range queries work.
/// Real timezone behaviour is covered by the Postgres integration tests.
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
    }
}
