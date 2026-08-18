using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    EmploymentType = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WorkArrangement = table.Column<int>(type: "integer", nullable: false),
                    ExperienceLevel = table.Column<int>(type: "integer", nullable: false),
                    RequiredSkills = table.Column<List<string>>(type: "text[]", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Vacancies = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_EmployerId",
                table: "Jobs",
                column: "EmployerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
