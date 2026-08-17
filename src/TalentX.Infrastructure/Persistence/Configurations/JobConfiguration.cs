using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
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
    }
}
