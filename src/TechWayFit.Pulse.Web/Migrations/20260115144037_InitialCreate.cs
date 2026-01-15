using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechWayFit.Pulse.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Prompt = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ConfigJson = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContributionCounters",
                columns: table => new
                {
                    ParticipantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TotalContributions = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionCounters", x => x.ParticipantId);
                });

            migrationBuilder.CreateTable(
                name: "FacilitatorUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilitatorUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginOtps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    OtpCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginOtps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    IsAnonymous = table.Column<bool>(type: "INTEGER", nullable: false),
                    DimensionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Responses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActivityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    DimensionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Responses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentGroupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FacilitatorUserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Goal = table.Column<string>(type: "TEXT", nullable: true),
                    ContextJson = table.Column<string>(type: "TEXT", nullable: true),
                    SettingsJson = table.Column<string>(type: "TEXT", nullable: false),
                    JoinFormSchemaJson = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentActivityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FacilitatorUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_SessionId_Order",
                table: "Activities",
                columns: new[] { "SessionId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_SessionId_Status",
                table: "Activities",
                columns: new[] { "SessionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ContributionCounters_SessionId",
                table: "ContributionCounters",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_FacilitatorUsers_CreatedAt",
                table: "FacilitatorUsers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FacilitatorUsers_Email",
                table: "FacilitatorUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginOtps_Email_CreatedAt",
                table: "LoginOtps",
                columns: new[] { "Email", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginOtps_Email_OtpCode",
                table: "LoginOtps",
                columns: new[] { "Email", "OtpCode" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginOtps_ExpiresAt",
                table: "LoginOtps",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_SessionId_JoinedAt",
                table: "Participants",
                columns: new[] { "SessionId", "JoinedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Responses_ParticipantId_CreatedAt",
                table: "Responses",
                columns: new[] { "ParticipantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Responses_SessionId_ActivityId_CreatedAt",
                table: "Responses",
                columns: new[] { "SessionId", "ActivityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionGroups_FacilitatorUserId",
                table: "SessionGroups",
                column: "FacilitatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionGroups_FacilitatorUserId_Level_ParentGroupId",
                table: "SessionGroups",
                columns: new[] { "FacilitatorUserId", "Level", "ParentGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionGroups_ParentGroupId",
                table: "SessionGroups",
                column: "ParentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Code",
                table: "Sessions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ExpiresAt",
                table: "Sessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_FacilitatorUserId",
                table: "Sessions",
                column: "FacilitatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_GroupId",
                table: "Sessions",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Status",
                table: "Sessions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "ContributionCounters");

            migrationBuilder.DropTable(
                name: "FacilitatorUsers");

            migrationBuilder.DropTable(
                name: "LoginOtps");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.DropTable(
                name: "Responses");

            migrationBuilder.DropTable(
                name: "SessionGroups");

            migrationBuilder.DropTable(
                name: "Sessions");
        }
    }
}
