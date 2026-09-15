using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployerVerificationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.Sql("UPDATE \"Employers\" SET \"VerificationStatus\" = 'Verified', \"VerifiedAt\" = NOW() WHERE \"VerificationStatus\" = 'PendingVerification';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
