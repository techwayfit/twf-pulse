using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechWayFit.Pulse.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDefaultToSessionGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "SessionGroups",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "SessionGroups");
        }
    }
}
