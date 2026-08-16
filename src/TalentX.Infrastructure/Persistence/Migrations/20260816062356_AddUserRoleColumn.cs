using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "DomainUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "DomainUsers");
        }
    }
}
