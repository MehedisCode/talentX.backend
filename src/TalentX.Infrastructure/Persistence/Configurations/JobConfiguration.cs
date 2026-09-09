using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
            .IsRequired();

        builder.Property(j => j.CompanyName)
            .IsRequired();

        builder.Property(j => j.Description)
            .IsRequired();

        builder.Property(j => j.Location)
            .IsRequired();

        builder.Property(j => j.ExpireAt)
            .IsRequired();

        builder.Property(j => j.EmploymentType).HasConversion<string>();
        builder.Property(j => j.WorkArrangement)
            .HasConversion(
                arrangement => arrangement.ToString(),
                value => value == "Remote" || value == "AnywhereFullRemote"
                    ? WorkArrangement.FullRemote
                    : Enum.Parse<WorkArrangement>(value));
        builder.Property(j => j.ExperienceLevel).HasConversion<string>();
        builder.Property(j => j.Status).HasConversion<string>().IsRequired();

        builder.Property(job => job.City).HasMaxLength(100);
        builder.Property(job => job.Country).HasMaxLength(100);
        builder.Property(job => job.MinOverlapHours);
        builder.Property(job => job.Responsibilities);

        var stringListComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == c2,
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property(j => j.RequiredSkills)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(stringListComparer);

        builder.Property(j => j.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(j => j.DeletedAt).IsRequired(false);
        builder.Property(j => j.PublishedAt).IsRequired(false);

        builder.HasQueryFilter(j => !j.IsDeleted);

        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.ExpireAt);
        builder.HasIndex(j => new { j.Status, j.CreatedAt });
        builder.HasIndex(j => new { j.Status, j.PublishedAt });
        builder.HasIndex(j => new { j.EmployerId, j.IsDeleted });
    }
}

