using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddJobReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    AdditionalDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AdminNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResolvedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobReports_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobReports_JobId_ReporterUserId",
                table: "JobReports",
                columns: new[] { "JobId", "ReporterUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobReports_ReporterUserId",
                table: "JobReports",
                column: "ReporterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobReports_Status",
                table: "JobReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JobReports_Status_CreatedAt",
                table: "JobReports",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobReports");
        }
    }
}
