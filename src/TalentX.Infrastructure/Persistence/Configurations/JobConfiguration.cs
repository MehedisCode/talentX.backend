using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;
<<<<<<< HEAD
using TalentX.Domain.Enums;
=======
>>>>>>> origin/TX26-192-employer-create-publish-job

namespace TalentX.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
<<<<<<< HEAD
        builder.Property(j => j.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(j => j.Title).HasMaxLength(200).IsRequired();
        builder.Property(j => j.City).HasMaxLength(100);
        builder.Property(j => j.Country).HasMaxLength(100);

        builder.Property(j => j.RoleType).HasConversion<string>();
        builder.Property(j => j.ExperienceLevel).HasConversion<string>();
        builder.Property(j => j.LocationPref).HasConversion<string>();
        builder.Property(j => j.Status).HasConversion<string>().IsRequired();

        builder.Property(j => j.SkillRequirements)
            .HasField("_skillRequirements")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.ExpiresAt);
        builder.HasIndex(j => new { j.Status, j.CreatedAt });
=======
        builder.ToTable("Jobs");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.EmployerId)
            .IsRequired();

        builder.HasIndex(j => j.EmployerId);

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.Description)
            .IsRequired()
            .HasMaxLength(10000);

        builder.Property(j => j.Location)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.ExpireAt)
            .IsRequired();

        // RequiredSkills list stored as a JSON or a separate table. Since EF Core 8 supports primitive collections,
        // we can store List<string> as JSON if we use HasConversion or simply it's supported depending on the provider.
        // For SQL Server in EF Core 8+, primitive collections are mapped to JSON arrays by default.
        // No explicit configuration needed for primitive collection unless needed.
>>>>>>> origin/TX26-192-employer-create-publish-job
    }
}
