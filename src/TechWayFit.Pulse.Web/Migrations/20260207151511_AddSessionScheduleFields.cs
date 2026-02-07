using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechWayFit.Pulse.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionScheduleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SessionEnd",
                table: "Sessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SessionStart",
                table: "Sessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "SessionGroups",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "SessionGroups",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Activities",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FacilitatorUserData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FacilitatorUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilitatorUserData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacilitatorUserData_FacilitatorUserId",
                table: "FacilitatorUserData",
                column: "FacilitatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FacilitatorUserData_FacilitatorUserId_Key",
                table: "FacilitatorUserData",
                columns: new[] { "FacilitatorUserId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacilitatorUserData");

            migrationBuilder.DropColumn(
                name: "SessionEnd",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SessionStart",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "SessionGroups");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "SessionGroups");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Activities");
        }
    }
}
