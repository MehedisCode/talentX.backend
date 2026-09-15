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
            // Every account (Talent/Employer/Guest/Admin) defaults to "Active"; only an explicit
            // Admin deactivation moves it to "Deactivated". Existing rows are backfilled to
            // "Active" via this column default so no currently-working account is disrupted.
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
