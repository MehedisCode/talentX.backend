using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountStatusToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows (including Employers, whose in-code default is now "Pending") are
            // backfilled to "Active" — enforcement of Pending/Deactivated access restrictions is
            // out of scope for this ticket, so no currently-working account should be disrupted.
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DomainUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "DomainUsers");
        }
    }
}
