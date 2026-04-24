using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TennisClub.Api.Domain.Entities;

namespace TennisClub.Api.Infrastructure.Persistence.Configurations;

public class CourtConfiguration : IEntityTypeConfiguration<Court>
{
    public void Configure(EntityTypeBuilder<Court> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(50).IsRequired();

        builder.HasIndex(c => c.DisplayOrder);
    }
}
