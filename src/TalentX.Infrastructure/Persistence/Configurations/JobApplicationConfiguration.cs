using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("JobApplications");
        builder.HasKey(application => application.Id);

        builder.Property(application => application.JobId)
            .IsRequired();

        builder.Property(application => application.TalentUserId)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(application => application.ApplicantName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(application => application.ApplicantEmail)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(application => application.ResumeStorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(application => application.CoverLetter)
            .HasMaxLength(4000);

        builder.Property(application => application.Location)
            .HasMaxLength(200);

        builder.Property(application => application.ExpectedSalary)
            .HasPrecision(12, 2);

        builder.Property(application => application.TotalYearsExperience)
            .HasPrecision(4, 1);

        builder.Property(application => application.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(application => application.AiMatchScore)
            .HasPrecision(5, 2);

        builder.Property(application => application.AiScoreInputHash)
            .HasMaxLength(64);

        builder.Property(application => application.AppliedAt)
            .IsRequired();

        builder.Property(application => application.CreatedAt)
            .IsRequired();

        builder.Property(application => application.ExtractedTechnicalSkillsJson)
            .HasColumnType("text");

        builder.Property(application => application.ExtractedSoftSkillsJson)
            .HasColumnType("text");

        builder.Property(application => application.ExtractedToolsAndPlatformsJson)
            .HasColumnType("text");

        builder.Property(application => application.ExtractedWorkExperienceJson)
            .HasColumnType("text");

        builder.Property(application => application.ExtractedEducationJson)
            .HasColumnType("text");

        builder.HasOne<Job>()
            .WithMany()
            .HasForeignKey(application => application.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(application => application.JobId);
        builder.HasIndex(application => application.TalentUserId);
    }
}
