using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewInvitationsAndEmailDispatchLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailDispatchLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    EmailType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailDispatchLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterviewInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TalentUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssessmentUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AssessmentDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LocationOrLink = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DeclineReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SelectedTimeslotId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewInvitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterviewTimeslots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InterviewInvitationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MeetingUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsBooked = table.Column<bool>(type: "boolean", nullable: false),
                    BookedByTalentUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewTimeslots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewTimeslots_InterviewInvitations_InterviewInvitation~",
                        column: x => x.InterviewInvitationId,
                        principalTable: "InterviewInvitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchLogs_Status_NextAttemptAt",
                table: "EmailDispatchLogs",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewInvitations_EmployerUserId",
                table: "InterviewInvitations",
                column: "EmployerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewInvitations_JobApplicationId",
                table: "InterviewInvitations",
                column: "JobApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewInvitations_JobApplicationId_Status",
                table: "InterviewInvitations",
                columns: new[] { "JobApplicationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewInvitations_JobId",
                table: "InterviewInvitations",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewInvitations_TalentUserId",
                table: "InterviewInvitations",
                column: "TalentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewTimeslots_InterviewInvitationId",
                table: "InterviewTimeslots",
                column: "InterviewInvitationId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewTimeslots_IsBooked",
                table: "InterviewTimeslots",
                column: "IsBooked");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailDispatchLogs");

            migrationBuilder.DropTable(
                name: "InterviewTimeslots");

            migrationBuilder.DropTable(
                name: "InterviewInvitations");
        }
    }
}
