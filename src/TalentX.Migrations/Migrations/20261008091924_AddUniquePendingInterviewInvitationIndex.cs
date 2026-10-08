using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddUniquePendingInterviewInvitationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One-time data fix: cancel every Pending invite except the most-recently-sent one
            // per application (matches this feature's own "most recent wins" logic in
            // GetByApplicationIdAsync), so the unique filtered index below can be created even if
            // duplicate-Pending rows already exist from before this constraint was added.
            migrationBuilder.Sql(
                """
                UPDATE "InterviewInvitations" AS ii
                SET "Status" = 'Cancelled', "UpdatedAt" = NOW()
                WHERE ii."Status" = 'Pending'
                  AND ii."Id" <> (
                    SELECT i2."Id" FROM "InterviewInvitations" i2
                    WHERE i2."JobApplicationId" = ii."JobApplicationId" AND i2."Status" = 'Pending'
                    ORDER BY i2."SentAt" DESC, i2."Id" DESC
                    LIMIT 1
                  );
                """);

            migrationBuilder.DropIndex(
                name: "IX_InterviewInvitations_JobApplicationId",
                table: "InterviewInvitations");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewInvitations_JobApplicationId_Pending_Unique",
                table: "InterviewInvitations",
                column: "JobApplicationId",
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InterviewInvitations_JobApplicationId_Pending_Unique",
                table: "InterviewInvitations");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewInvitations_JobApplicationId",
                table: "InterviewInvitations",
                column: "JobApplicationId");
        }
    }
}
