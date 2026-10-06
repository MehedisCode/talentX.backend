using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentX.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class BackfillExistingEmployerCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    emp_record RECORD;
                    candidate_name TEXT;
                    final_name TEXT;
                    suffix_counter INT;
                BEGIN
                    FOR emp_record IN
                        SELECT
                            e.""Id"" AS ""EmployerEntityId"",
                            u.""UserId"" AS ""IdentityUserId"",
                            u.""Email"" AS ""UserEmail"",
                            u.""Name"" AS ""UserName"",
                            e.""CompanyName"" AS ""EmployerCompanyName"",
                            u.""CreatedAt"" AS ""UserCreatedAt"",
                            (
                                SELECT j.""CompanyName""
                                FROM ""Jobs"" j
                                WHERE j.""EmployerId"" = u.""UserId"" AND NULLIF(TRIM(j.""CompanyName""), '') IS NOT NULL
                                ORDER BY j.""CreatedAt"" DESC
                                LIMIT 1
                            ) AS ""LatestJobCompanyName""
                        FROM ""Employers"" e
                        JOIN ""DomainUsers"" u ON u.""Id"" = e.""Id""
                        WHERE NOT EXISTS (
                            SELECT 1 FROM ""Companies"" c WHERE c.""EmployerId"" = u.""UserId""
                        )
                        ORDER BY u.""CreatedAt"" ASC
                    LOOP
                        -- Precedence: Employers.CompanyName -> Latest Jobs.CompanyName -> UserEmail -> 'Company'
                        candidate_name := COALESCE(
                            NULLIF(TRIM(emp_record.""EmployerCompanyName""), ''),
                            NULLIF(TRIM(emp_record.""LatestJobCompanyName""), ''),
                            NULLIF(TRIM(emp_record.""UserEmail""), ''),
                            'Company'
                        );

                        final_name := candidate_name;
                        suffix_counter := 1;

                        -- Ensure uniqueness against existing and inserted Companies (case-insensitive)
                        WHILE EXISTS (SELECT 1 FROM ""Companies"" c WHERE LOWER(c.""Name"") = LOWER(final_name)) LOOP
                            suffix_counter := suffix_counter + 1;
                            final_name := candidate_name || ' (' || suffix_counter || ')';
                        END LOOP;

                        -- Insert backfilled Company record
                        INSERT INTO ""Companies"" (
                            ""Id"",
                            ""EmployerId"",
                            ""Name"",
                            ""LogoStorageKey"",
                            ""Description"",
                            ""Industry"",
                            ""CompanySize"",
                            ""Website"",
                            ""Location"",
                            ""LinkedInUrl"",
                            ""TwitterUrl"",
                            ""FacebookUrl"",
                            ""CreatedAt"",
                            ""UpdatedAt""
                        ) VALUES (
                            gen_random_uuid(),
                            emp_record.""IdentityUserId"",
                            final_name,
                            NULL,
                            NULL,
                            NULL,
                            NULL,
                            NULL,
                            NULL,
                            NULL,
                            NULL,
                            NULL,
                            COALESCE(emp_record.""UserCreatedAt"", NOW()),
                            NULL
                        );
                    END LOOP;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data backfill migration. Dropping backfilled rows is non-trivial and could delete live user updates.
            // Reversal should only occur via targeted backup restoration if required.
        }
    }
}
