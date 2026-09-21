using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddExtractionDataToJobApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExtractedEducationJson",
                table: "JobApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedSoftSkillsJson",
                table: "JobApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedTechnicalSkillsJson",
                table: "JobApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedToolsAndPlatformsJson",
                table: "JobApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedWorkExperienceJson",
                table: "JobApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Employers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Employers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationStatus",
                table: "Employers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "PendingVerification");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "Employers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtractedEducationJson",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ExtractedSoftSkillsJson",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ExtractedTechnicalSkillsJson",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ExtractedToolsAndPlatformsJson",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ExtractedWorkExperienceJson",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Employers");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Employers");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "Employers");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "Employers");
        }
    }
}
