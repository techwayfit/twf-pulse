using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechWayFit.Pulse.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "Participants",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Token",
                table: "Participants");
        }
    }
}
