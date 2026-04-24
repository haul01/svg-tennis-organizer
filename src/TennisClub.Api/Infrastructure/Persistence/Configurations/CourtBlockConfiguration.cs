using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TennisClub.Api.Domain.Entities;

namespace TennisClub.Api.Infrastructure.Persistence.Configurations;

public class CourtBlockConfiguration : IEntityTypeConfiguration<CourtBlock>
{
    public void Configure(EntityTypeBuilder<CourtBlock> builder)
    {
        builder.Property(b => b.Reason).HasMaxLength(200).IsRequired();

        builder.HasOne(b => b.Court)
            .WithMany()
            .HasForeignKey(b => b.CourtId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.CourtId, b.StartsAt });
        builder.HasIndex(b => b.SeriesId);
    }
}
