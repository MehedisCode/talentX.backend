using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class JobAlertNotificationHistoryConfiguration : IEntityTypeConfiguration<JobAlertNotificationHistory>
{
    public void Configure(EntityTypeBuilder<JobAlertNotificationHistory> builder)
    {
        builder.ToTable("JobAlertNotificationHistories");
        builder.HasKey(history => history.Id);

        builder.Property(history => history.TalentId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(history => history.JobId)
            .IsRequired();

        builder.Property(history => history.JobAlertId)
            .IsRequired();

        builder.Property(history => history.SentAt)
            .IsRequired();

        builder.Property(history => history.CreatedAt)
            .IsRequired();

        builder.HasOne<Job>()
            .WithMany()
            .HasForeignKey(history => history.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<JobAlert>()
            .WithMany()
            .HasForeignKey(history => history.JobAlertId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(history => new { history.TalentId, history.JobId })
            .IsUnique();

        builder.HasIndex(history => history.JobId);
    }
}
