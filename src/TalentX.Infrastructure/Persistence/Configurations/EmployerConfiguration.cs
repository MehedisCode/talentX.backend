using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain;
using TalentX.Domain.Enums;

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

        builder.Property(e => e.VerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(EmployerVerificationStatus.PendingVerification)
            .IsRequired();

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(e => e.VerifiedAt)
            .IsRequired(false);

        builder.Property(e => e.RejectedAt)
            .IsRequired(false);
    }
}
