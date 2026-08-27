using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class EducationConfiguration : IEntityTypeConfiguration<Education>
{
    public void Configure(EntityTypeBuilder<Education> builder)
    {
        builder.ToTable("Educations");
        builder.HasKey(education => education.Id);

        builder.Property(education => education.UserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(education => education.UserId);

        builder.Property(education => education.Institution)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(education => education.Degree)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(education => education.StartDate)
            .HasMaxLength(50);

        builder.Property(education => education.EndDate)
            .HasMaxLength(50);

        builder.Property(education => education.CreatedAt)
            .IsRequired();
    }
}
