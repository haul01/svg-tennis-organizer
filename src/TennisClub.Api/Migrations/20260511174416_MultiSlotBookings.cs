using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TennisClub.Api.Migrations
{
    /// <inheritdoc />
    public partial class MultiSlotBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The old filtered unique index only stopped same-start dupes.
            // It is replaced below by a GiST EXCLUDE constraint that also
            // catches overlapping reservations whose StartsAt differs.
            migrationBuilder.DropIndex(
                name: "IX_Reservations_CourtId_StartsAt",
                table: "Reservations");

            // Default 4 = 4 × slot covers the 2 h case out of the box,
            // whether the season is on 30 min or 60 min slots.
            migrationBuilder.AddColumn<int>(
                name: "MaxSlotsPerBooking",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            // EF needs the simple non-unique FK index it would otherwise
            // have inferred from the dropped unique index.
            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CourtId",
                table: "Reservations",
                column: "CourtId");

            // Overlap guard. btree_gist lets the int equality on CourtId
            // sit alongside the range overlap (&&) on (StartsAt, EndsAt)
            // inside one GiST index, which Postgres enforces as EXCLUDE.
            migrationBuilder.Sql(@"
                CREATE EXTENSION IF NOT EXISTS btree_gist;

                ALTER TABLE ""Reservations""
                    ADD CONSTRAINT no_overlapping_reservations
                    EXCLUDE USING gist (
                        ""CourtId"" WITH =,
                        tstzrange(""StartsAt"", ""EndsAt"") WITH &&
                    ) WHERE (""Status"" = 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Reservations""
                    DROP CONSTRAINT IF EXISTS no_overlapping_reservations;
            ");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_CourtId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "MaxSlotsPerBooking",
                table: "SystemSettings");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CourtId_StartsAt",
                table: "Reservations",
                columns: new[] { "CourtId", "StartsAt" },
                unique: true,
                filter: "\"Status\" = 0");
        }
    }
}
