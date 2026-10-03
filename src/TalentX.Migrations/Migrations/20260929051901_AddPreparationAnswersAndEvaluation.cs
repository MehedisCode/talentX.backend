using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPreparationAnswersAndEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterviewPreparationEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InterviewPreparationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallScore = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Strengths = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    AreasToImprove = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    QuestionFeedback = table.Column<string>(type: "text", nullable: false),
                    AnsweredCount = table.Column<int>(type: "integer", nullable: false),
                    TotalCount = table.Column<int>(type: "integer", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewPreparationEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewPreparationEvaluations_InterviewPreparations_Inter~",
                        column: x => x.InterviewPreparationId,
                        principalTable: "InterviewPreparations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreparationAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InterviewPreparationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QuestionIndex = table.Column<int>(type: "integer", nullable: false),
                    QuestionText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AnswerText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IsPracticed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparationAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreparationAnswers_InterviewPreparations_InterviewPreparati~",
                        column: x => x.InterviewPreparationId,
                        principalTable: "InterviewPreparations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPreparationEvaluations_InterviewPreparationId",
                table: "InterviewPreparationEvaluations",
                column: "InterviewPreparationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreparationAnswers_InterviewPreparationId_Category_Question~",
                table: "PreparationAnswers",
                columns: new[] { "InterviewPreparationId", "Category", "QuestionIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewPreparationEvaluations");

            migrationBuilder.DropTable(
                name: "PreparationAnswers");
        }
    }
}
