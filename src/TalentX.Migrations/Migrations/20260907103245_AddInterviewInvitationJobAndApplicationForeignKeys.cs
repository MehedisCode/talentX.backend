using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewInvitationJobAndApplicationForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_InterviewInvitations_JobApplications_JobApplicationId",
                table: "InterviewInvitations",
                column: "JobApplicationId",
                principalTable: "JobApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewInvitations_Jobs_JobId",
                table: "InterviewInvitations",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewInvitations_JobApplications_JobApplicationId",
                table: "InterviewInvitations");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewInvitations_Jobs_JobId",
                table: "InterviewInvitations");
        }
    }
}
