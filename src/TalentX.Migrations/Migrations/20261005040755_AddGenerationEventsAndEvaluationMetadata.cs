using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddGenerationEventsAndEvaluationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TechnicalQuestions",
                table: "InterviewPreparations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000);

            migrationBuilder.AlterColumn<string>(
                name: "SuggestedRevisionAreas",
                table: "InterviewPreparations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "RoleSpecificQuestions",
                table: "InterviewPreparations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000);

            migrationBuilder.AlterColumn<string>(
                name: "KeyTopicsToPrepare",
                table: "InterviewPreparations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "ExperienceBasedQuestions",
                table: "InterviewPreparations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000);

            migrationBuilder.AlterColumn<string>(
                name: "BehavioralQuestions",
                table: "InterviewPreparations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000);

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalQuestions",
                table: "InterviewPreparations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Summary",
                table: "InterviewPreparationEvaluations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "Strengths",
                table: "InterviewPreparationEvaluations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000);

            migrationBuilder.AlterColumn<string>(
                name: "AreasToImprove",
                table: "InterviewPreparationEvaluations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000);

            migrationBuilder.AddColumn<int>(
                name: "InputTokenCount",
                table: "InterviewPreparationEvaluations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LatencyMs",
                table: "InterviewPreparationEvaluations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "InterviewPreparationEvaluations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OutputTokenCount",
                table: "InterviewPreparationEvaluations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptVersion",
                table: "InterviewPreparationEvaluations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InterviewPreparationGenerations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InterviewPreparationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TalentUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewPreparationGenerations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewPreparationGenerations_InterviewPreparations_Inter~",
                        column: x => x.InterviewPreparationId,
                        principalTable: "InterviewPreparations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPreparationGenerations_InterviewPreparationId",
                table: "InterviewPreparationGenerations",
                column: "InterviewPreparationId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPreparationGenerations_TalentUserId",
                table: "InterviewPreparationGenerations",
                column: "TalentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPreparationGenerations_TalentUserId_CreatedAt",
                table: "InterviewPreparationGenerations",
                columns: new[] { "TalentUserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewPreparationGenerations");

            migrationBuilder.DropColumn(
                name: "InputTokenCount",
                table: "InterviewPreparationEvaluations");

            migrationBuilder.DropColumn(
                name: "LatencyMs",
                table: "InterviewPreparationEvaluations");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "InterviewPreparationEvaluations");

            migrationBuilder.DropColumn(
                name: "OutputTokenCount",
                table: "InterviewPreparationEvaluations");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                table: "InterviewPreparationEvaluations");

            migrationBuilder.AlterColumn<string>(
                name: "TechnicalQuestions",
                table: "InterviewPreparations",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SuggestedRevisionAreas",
                table: "InterviewPreparations",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "RoleSpecificQuestions",
                table: "InterviewPreparations",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "KeyTopicsToPrepare",
                table: "InterviewPreparations",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ExperienceBasedQuestions",
                table: "InterviewPreparations",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "BehavioralQuestions",
                table: "InterviewPreparations",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalQuestions",
                table: "InterviewPreparations",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Summary",
                table: "InterviewPreparationEvaluations",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Strengths",
                table: "InterviewPreparationEvaluations",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AreasToImprove",
                table: "InterviewPreparationEvaluations",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
