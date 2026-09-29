using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewPreparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterviewPreparations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InterviewInvitationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TalentUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InterviewType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TechnicalQuestions = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    BehavioralQuestions = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    ExperienceBasedQuestions = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    RoleSpecificQuestions = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    KeyTopicsToPrepare = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SuggestedRevisionAreas = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AdditionalQuestions = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    GenerationCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewPreparations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewPreparations_InterviewInvitations_InterviewInvitat~",
                        column: x => x.InterviewInvitationId,
                        principalTable: "InterviewInvitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewPreparations_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPreparations_InterviewInvitationId",
                table: "InterviewPreparations",
                column: "InterviewInvitationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPreparations_JobId",
                table: "InterviewPreparations",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPreparations_TalentUserId",
                table: "InterviewPreparations",
                column: "TalentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPreparations_TalentUserId_JobId",
                table: "InterviewPreparations",
                columns: new[] { "TalentUserId", "JobId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewPreparations");
        }
    }
}
