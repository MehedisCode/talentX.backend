using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class JobAlertConfiguration : IEntityTypeConfiguration<JobAlert>
{
    public void Configure(EntityTypeBuilder<JobAlert> builder)
    {
        builder.ToTable("JobAlerts");
        builder.HasKey(alert => alert.Id);

        builder.Property(alert => alert.TalentId)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(alert => alert.TalentId);

        builder.Property(alert => alert.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(alert => alert.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(alert => alert.Location)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(alert => alert.EmploymentType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(alert => alert.ExperienceLevel)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(alert => alert.WorkArrangement)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(alert => alert.IsEnabled)
            .IsRequired();

        builder.Property(alert => alert.CreatedAt)
            .IsRequired();

        builder.HasIndex(alert => new { alert.EmploymentType, alert.ExperienceLevel, alert.WorkArrangement })
            .HasFilter("\"IsEnabled\" = true")
            .HasDatabaseName("IX_JobAlerts_Active_Matching");
    }
}
