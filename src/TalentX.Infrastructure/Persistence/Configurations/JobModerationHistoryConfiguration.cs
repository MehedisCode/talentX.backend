using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class JobModerationHistoryConfiguration : IEntityTypeConfiguration<JobModerationHistory>
{
    public void Configure(EntityTypeBuilder<JobModerationHistory> builder)
    {
        builder.ToTable("JobModerationHistories");
        builder.HasKey(history => history.Id);

        builder.Property(history => history.JobId)
            .IsRequired();

        builder.Property(history => history.Action)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(history => history.FromStatus)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(history => history.ToStatus)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(history => history.Reason)
            .HasConversion<string>();

        builder.Property(history => history.Details)
            .HasMaxLength(1000);

        builder.Property(history => history.ActorUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(history => history.ActorEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(history => history.CreatedAt)
            .IsRequired();

        // Restrict, not Cascade: jobs are only soft-deleted, and moderation history is
        // the record of admin actions, so it must never disappear with its job.
        builder.HasOne<Job>()
            .WithMany()
            .HasForeignKey(history => history.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(history => new { history.JobId, history.CreatedAt });
    }
}
