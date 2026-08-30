using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <summary>
    /// Adds wizard fields (City, Country, RequiredTimezoneOverlap) to Jobs table
    /// for the multi-step job posting feature.
    /// </summary>
    public partial class AddWizardFieldsToJob : Migration
    {
        /// <summary>
        /// Adds nullable columns for the wizard fields.
        /// Existing data is not affected since all new columns are nullable.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredTimezoneOverlap",
                table: "Jobs",
                type: "text",
                nullable: true);
        }

        /// <summary>
        /// Removes the wizard columns.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RequiredTimezoneOverlap",
                table: "Jobs");
        }
    }
}
