using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechWayFit.Pulse.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    IconEmoji = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ConfigJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionTemplates_Category",
                table: "SessionTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SessionTemplates_CreatedByUserId",
                table: "SessionTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionTemplates_IsSystemTemplate",
                table: "SessionTemplates",
                column: "IsSystemTemplate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionTemplates");
        }
    }
}
