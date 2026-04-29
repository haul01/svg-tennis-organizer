using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TennisClub.Api.Domain.Entities;

namespace TennisClub.Api.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.Property(r => r.Status).HasConversion<int>();

        builder.HasOne(r => r.Court)
            .WithMany()
            .HasForeignKey(r => r.CourtId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Member)
            .WithMany(m => m.Reservations)
            .HasForeignKey(r => r.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.GuestPlayer)
            .WithMany()
            .HasForeignKey(r => r.GuestPlayerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Filtered unique index - primary defense against double-booking.
        // Only active reservations must be unique per (Court, StartsAt).
        // Postgres requires double-quoted identifiers for case-sensitive
        // PascalCase column names.
        builder.HasIndex(r => new { r.CourtId, r.StartsAt })
            .HasFilter("\"Status\" = 0")
            .IsUnique();

        // Query index for "reservations for week" (week-grid query).
        builder.HasIndex(r => new { r.StartsAt, r.CourtId })
            .HasFilter("\"Status\" = 0");

        // Query index for "my reservations".
        builder.HasIndex(r => new { r.MemberId, r.Status, r.StartsAt });
    }
}
