using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddJobSalaryAndRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Requirements",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalaryCurrency",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalaryMax",
                table: "Jobs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalaryMin",
                table: "Jobs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalaryPeriod",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalaryType",
                table: "Jobs",
                type: "text",
                nullable: true);

            // NOTE: this migration intentionally does NOT touch the "Invitations"
            // table. EF's diff also proposed re-adding ExpiresAt/InvitationCount/
            // Message/Status/Token there, but those columns already exist in the
            // real database (added by the already-applied migration
            // 20260907075803_AddInvitationAndMatchStatus) — the model snapshot is
            // just stale about it (pre-existing issue, unrelated to Job salary/
            // requirements; confirmed via `dotnet ef migrations list` showing
            // AddInvitationAndMatchStatus with no "(Pending)" marker). Applying
            // those AddColumn statements here would fail with a duplicate-column
            // error. Left for a separate, dedicated fix.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Requirements",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SalaryCurrency",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SalaryMax",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SalaryMin",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SalaryPeriod",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SalaryType",
                table: "Jobs");
        }
    }
}
