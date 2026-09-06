using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class SavedJobConfiguration : IEntityTypeConfiguration<SavedJob>
{
    public void Configure(EntityTypeBuilder<SavedJob> builder)
    {
        builder.ToTable("SavedJobs");
        builder.HasKey(savedJob => savedJob.Id);

        builder.Property(savedJob => savedJob.JobId)
            .IsRequired();

        builder.Property(savedJob => savedJob.TalentId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(savedJob => savedJob.CreatedAt)
            .IsRequired();

        builder.HasOne<Job>()
            .WithMany()
            .HasForeignKey(savedJob => savedJob.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(savedJob => new { savedJob.TalentId, savedJob.JobId })
            .IsUnique();

        builder.HasIndex(savedJob => savedJob.TalentId);
    }
}
