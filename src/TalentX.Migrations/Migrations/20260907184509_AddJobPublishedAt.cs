using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPublishedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill: existing jobs that already went live (Published or Closed —
            // Closed is only reachable from Published) never had a PublishedAt
            // recorded. CreatedAt is the closest available approximation for them.
            // Jobs still in Draft correctly stay NULL — they were never published.
            migrationBuilder.Sql(@"
                UPDATE ""Jobs""
                SET ""PublishedAt"" = ""CreatedAt""
                WHERE ""Status"" IN ('Published', 'Closed') AND ""PublishedAt"" IS NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Status_PublishedAt",
                table: "Jobs",
                columns: new[] { "Status", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_Status_PublishedAt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Jobs");
        }
    }
}
