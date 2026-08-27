using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class WorkExperienceConfiguration : IEntityTypeConfiguration<WorkExperience>
{
    public void Configure(EntityTypeBuilder<WorkExperience> builder)
    {
        builder.ToTable("WorkExperiences");
        builder.HasKey(workExperience => workExperience.Id);

        builder.Property(workExperience => workExperience.UserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(workExperience => workExperience.UserId);

        builder.Property(workExperience => workExperience.CompanyName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(workExperience => workExperience.JobTitle)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(workExperience => workExperience.StartDate)
            .HasMaxLength(50);

        builder.Property(workExperience => workExperience.EndDate)
            .HasMaxLength(50);

        builder.Property(workExperience => workExperience.IsCurrent)
            .IsRequired();

        builder.Property(workExperience => workExperience.CreatedAt)
            .IsRequired();
    }
}
