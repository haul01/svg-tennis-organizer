using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TennisClub.Api.Domain.Entities;

namespace TennisClub.Api.Infrastructure.Persistence.Configurations;

public class GuestPlayerConfiguration : IEntityTypeConfiguration<GuestPlayer>
{
    public void Configure(EntityTypeBuilder<GuestPlayer> builder)
    {
        builder.Property(g => g.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(g => g.LastName).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Email).HasMaxLength(256);

        builder.HasOne(g => g.InvitedBy)
            .WithMany()
            .HasForeignKey(g => g.InvitedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => new { g.InvitedByMemberId, g.IsActive });
    }
}
