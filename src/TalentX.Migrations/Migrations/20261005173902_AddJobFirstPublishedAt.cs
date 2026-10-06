using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddJobFirstPublishedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstPublishedAt",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: true);

            // Existing jobs: the latest publish date is the best record left, since reopen/reactivate overwrote the
            // original one.
            migrationBuilder.Sql(
                "UPDATE \"Jobs\" SET \"FirstPublishedAt\" = \"PublishedAt\" WHERE \"PublishedAt\" IS NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_FirstPublishedAt",
                table: "Jobs",
                column: "FirstPublishedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_FirstPublishedAt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "FirstPublishedAt",
                table: "Jobs");
        }
    }
}
