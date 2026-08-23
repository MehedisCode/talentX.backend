using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain;

namespace TalentX.Infrastructure.Persistence.Configurations;

// Mapping Employer to its own table makes EF Core use Table-Per-Type (TPT) inheritance from User.
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
