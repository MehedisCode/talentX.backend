using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobApplications_JobId_TalentUserId",
                table: "JobApplications");

            migrationBuilder.AlterColumn<string>(
                name: "TalentUserId",
                table: "JobApplications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateTable(
                name: "EmailVerificationTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ApplicantEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    JobApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailVerificationTokens_JobApplications_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_ApplicantEmail",
                table: "EmailVerificationTokens",
                column: "ApplicantEmail");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_JobApplicationId",
                table: "EmailVerificationTokens",
                column: "JobApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_Token",
                table: "EmailVerificationTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailVerificationTokens");

            migrationBuilder.AlterColumn<string>(
                name: "TalentUserId",
                table: "JobApplications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_JobId_TalentUserId",
                table: "JobApplications",
                columns: new[] { "JobId", "TalentUserId" },
                unique: true);
        }
    }
}
