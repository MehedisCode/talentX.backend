using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class JobReportConfiguration : IEntityTypeConfiguration<JobReport>
{
    public void Configure(EntityTypeBuilder<JobReport> builder)
    {
        builder.ToTable("JobReports");
        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.Job)
            .WithMany()
            .HasForeignKey(r => r.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.ReporterUserId)
            .HasMaxLength(200)
            .IsRequired();
            
        builder.HasIndex(r => r.ReporterUserId);

        builder.Property(r => r.ResolvedByUserId)
            .HasMaxLength(200);

        builder.Property(r => r.Reason)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.AdditionalDetails)
            .HasMaxLength(1000);

        builder.Property(r => r.AdminNotes)
            .HasMaxLength(1000);

        builder.HasIndex(r => new { r.JobId, r.ReporterUserId })
            .IsUnique();

        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => new { r.Status, r.CreatedAt });
    }
}
