using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TennisClub.Api.Domain.Entities;

namespace TennisClub.Api.Infrastructure.Persistence.Configurations;

public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        // Single-row table - Id is fixed at 1 so seed/update is deterministic.
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.GuestMembershipPromptText)
            .HasMaxLength(2000)
            // Column default so the migration backfills the existing single
            // SystemSettings row with the friendly default instead of "".
            .HasDefaultValue(SystemSettings.DefaultGuestMembershipPromptText);
    }
}
