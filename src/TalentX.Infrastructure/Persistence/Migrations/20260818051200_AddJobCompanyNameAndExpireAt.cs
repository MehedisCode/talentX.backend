using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobCompanyNameAndExpireAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Jobs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpireAt",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ExpireAt",
                table: "Jobs");
        }
    }
}
