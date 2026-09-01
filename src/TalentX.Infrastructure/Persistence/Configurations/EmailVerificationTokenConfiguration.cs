using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("EmailVerificationTokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(t => t.ApplicantEmail)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.JobApplicationId)
            .IsRequired();

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        builder.Property(t => t.ConsumedAt)
            .IsRequired(false);

        builder.HasIndex(t => t.Token)
            .IsUnique();

        builder.HasIndex(t => t.ApplicantEmail);

        builder.HasOne<JobApplication>()
            .WithMany()
            .HasForeignKey(t => t.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
