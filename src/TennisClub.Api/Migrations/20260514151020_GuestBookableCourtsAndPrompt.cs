using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TennisClub.Api.Migrations
{
    /// <inheritdoc />
    public partial class GuestBookableCourtsAndPrompt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuestMembershipPromptText",
                table: "SystemSettings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "Schön, dass du bei uns spielst! Hast du schon überlegt, Vereinsmitglied zu werden? Als Mitglied kannst du alle Plätze buchen, hast bessere Buchungsbedingungen und unterstützt damit unseren Verein. Wir freuen uns über jede neue Mitgliedschaft!");

            migrationBuilder.AddColumn<bool>(
                name: "IsGuestBookable",
                table: "Courts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestMembershipPromptText",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "IsGuestBookable",
                table: "Courts");
        }
    }
}
