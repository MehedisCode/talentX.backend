using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddResumeLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resumes_UserId",
                table: "Resumes");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Resumes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Resumes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Before this migration UserId was unique, so every existing row is that
            // user's only resume: derive its display name from the file name and make
            // it the default.
            migrationBuilder.Sql(
                """
                UPDATE "Resumes"
                SET "DisplayName" = COALESCE(
                        NULLIF(LEFT(regexp_replace("OriginalFileName", '\.[^.]*$', ''), 100), ''),
                        'Resume'),
                    "IsDefault" = TRUE;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Resumes_UserId",
                table: "Resumes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resumes_UserId",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Resumes");

            migrationBuilder.CreateIndex(
                name: "IX_Resumes_UserId",
                table: "Resumes",
                column: "UserId",
                unique: true);
        }
    }
}
