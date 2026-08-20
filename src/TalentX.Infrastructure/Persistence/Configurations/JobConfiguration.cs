using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
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

        builder.Property(j => j.EmploymentType).HasConversion<string>();
        builder.Property(j => j.WorkArrangement).HasConversion<string>();
        builder.Property(j => j.ExperienceLevel).HasConversion<string>();
        builder.Property(j => j.Status).HasConversion<string>().IsRequired();

        builder.Property(j => j.RequiredSkills)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.ExpireAt);
        builder.HasIndex(j => new { j.Status, j.CreatedAt });
    }
}

