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

        // Double-booking protection is enforced by the GiST EXCLUDE
        // constraint added in the MultiSlotBookings migration (rejects
        // any overlapping active reservation on the same court). EF Core
        // can't model EXCLUDE constraints, so it isn't represented here.

        // Query index for "reservations for week" (week-grid query).
        builder.HasIndex(r => new { r.StartsAt, r.CourtId })
            .HasFilter("\"Status\" = 0");

        // Query index for "my reservations".
        builder.HasIndex(r => new { r.MemberId, r.Status, r.StartsAt });
    }
}
