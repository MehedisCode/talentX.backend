using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for <see cref="Employer"/>.
/// Maps Employer to Employers table using Table-Per-Type (TPT) inheritance.
/// </summary>
public sealed class EmployerConfiguration : IEntityTypeConfiguration<Employer>
{
    public void Configure(EntityTypeBuilder<Employer> builder)
    {
        builder.ToTable("Employers");

        builder.Property(e => e.CompanyName)
            .HasMaxLength(200)
            .IsRequired();
    }
}
