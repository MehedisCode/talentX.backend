using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(project => project.Id);

        builder.Property(project => project.UserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(project => project.UserId);

        builder.Property(project => project.ProjectName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(project => project.Role)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(project => project.ProjectType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(project => project.StartDate)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(project => project.EndDate)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(project => project.IsOngoing)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(project => project.ProjectUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        var stringListComparer = new ValueComparer<List<string>>(
            (firstList, secondList) => firstList != null && secondList != null ? firstList.SequenceEqual(secondList) : firstList == secondList,
            list => list.Aggregate(0, (accumulatedHash, item) => HashCode.Combine(accumulatedHash, item.GetHashCode())),
            list => list.ToList());

        builder.Property(project => project.TechnologiesUsed)
            .HasConversion(
                technologies => System.Text.Json.JsonSerializer.Serialize(technologies, (System.Text.Json.JsonSerializerOptions?)null),
                jsonString => System.Text.Json.JsonSerializer.Deserialize<List<string>>(jsonString, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(stringListComparer);

        builder.Property(project => project.CreatedAt)
            .IsRequired();
    }
}
