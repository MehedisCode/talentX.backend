using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddJobSearchAlertsAndNotificationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TalentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmploymentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExperienceLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WorkArrangement = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TalentNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TalentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JobAlertsEmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentNotificationPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobAlertNotificationHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TalentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobAlertId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobAlertNotificationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobAlertNotificationHistories_JobAlerts_JobAlertId",
                        column: x => x.JobAlertId,
                        principalTable: "JobAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobAlertNotificationHistories_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobAlertNotificationHistories_JobAlertId",
                table: "JobAlertNotificationHistories",
                column: "JobAlertId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAlertNotificationHistories_JobId",
                table: "JobAlertNotificationHistories",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAlertNotificationHistories_TalentId_JobId",
                table: "JobAlertNotificationHistories",
                columns: new[] { "TalentId", "JobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobAlerts_Active_Matching",
                table: "JobAlerts",
                columns: new[] { "EmploymentType", "ExperienceLevel", "WorkArrangement" },
                filter: "\"IsEnabled\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_JobAlerts_TalentId",
                table: "JobAlerts",
                column: "TalentId");

            migrationBuilder.CreateIndex(
                name: "IX_TalentNotificationPreferences_TalentId",
                table: "TalentNotificationPreferences",
                column: "TalentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobAlertNotificationHistories");

            migrationBuilder.DropTable(
                name: "TalentNotificationPreferences");

            migrationBuilder.DropTable(
                name: "JobAlerts");
        }
    }
}
