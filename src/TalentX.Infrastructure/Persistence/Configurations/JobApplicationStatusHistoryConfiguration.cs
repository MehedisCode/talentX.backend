using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class JobApplicationStatusHistoryConfiguration : IEntityTypeConfiguration<JobApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<JobApplicationStatusHistory> builder)
    {
        builder.ToTable("JobApplicationStatusHistories");
        builder.HasKey(history => history.Id);

        builder.Property(history => history.JobApplicationId)
            .IsRequired();

        builder.Property(history => history.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(history => history.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(history => history.ChangedByUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(history => history.ChangedAt)
            .IsRequired();

        builder.Property(history => history.CreatedAt)
            .IsRequired();

        // Cascade: history rows are meaningless without their application
        // (talent account deletion already removes applications).
        builder.HasOne<JobApplication>()
            .WithMany()
            .HasForeignKey(history => history.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(history => new { history.JobApplicationId, history.ChangedAt });
    }
}
