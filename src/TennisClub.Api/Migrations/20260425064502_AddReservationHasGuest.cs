using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TennisClub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationHasGuest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasGuest",
                table: "Reservations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasGuest",
                table: "Reservations");
        }
    }
}
