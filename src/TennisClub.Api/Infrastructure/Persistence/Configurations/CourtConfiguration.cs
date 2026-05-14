using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TennisClub.Api.Domain.Entities;

namespace TennisClub.Api.Infrastructure.Persistence.Configurations;

public class CourtConfiguration : IEntityTypeConfiguration<Court>
{
    public void Configure(EntityTypeBuilder<Court> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(50).IsRequired();

        // Existing rows added before the guest-bookable feature default
        // to closed-for-guests; admin opts in per court via the UI.
        builder.Property(c => c.IsGuestBookable).HasDefaultValue(false);

        builder.HasIndex(c => c.DisplayOrder);
    }
}
