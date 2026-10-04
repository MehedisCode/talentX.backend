using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class ResumeConfiguration : IEntityTypeConfiguration<Resume>
{
    public void Configure(EntityTypeBuilder<Resume> builder)
    {
        builder.ToTable("Resumes");
        builder.HasKey(resume => resume.Id);

        builder.Property(resume => resume.UserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(resume => resume.UserId);

        builder.Property(resume => resume.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(resume => resume.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(resume => resume.DisplayName)
            .HasMaxLength(Resume.MaxDisplayNameLength)
            .IsRequired();

        builder.Property(resume => resume.IsDefault)
            .IsRequired();

        builder.Property(resume => resume.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(resume => resume.SizeBytes)
            .IsRequired();

        builder.Property(resume => resume.UploadedAt)
            .IsRequired();

        builder.Property(resume => resume.CreatedAt)
            .IsRequired();
    }
}
