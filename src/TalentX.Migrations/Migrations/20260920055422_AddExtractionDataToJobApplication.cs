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
        }
    }
}
