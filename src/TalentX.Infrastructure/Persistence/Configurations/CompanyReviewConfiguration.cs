using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class CompanyReviewConfiguration : IEntityTypeConfiguration<CompanyReview>
{
    public void Configure(EntityTypeBuilder<CompanyReview> builder)
    {
        builder.ToTable("CompanyReviews");
        builder.HasKey(review => review.Id);

        builder.Property(review => review.CompanyId).IsRequired();
        builder.Property(review => review.JobId).IsRequired();
        builder.Property(review => review.JobApplicationId).IsRequired();

        builder.Property(review => review.TalentUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(review => review.JobTitle)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(review => review.Sentiment)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(review => review.Comment)
            .HasMaxLength(2000);

        builder.Property(review => review.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(review => review.ModerationReason)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(review => review.ModerationDetails)
            .HasMaxLength(1000);

        builder.Property(review => review.ModeratedByUserId)
            .HasMaxLength(200);

        builder.Property(review => review.SubmittedAt).IsRequired();
        builder.Property(review => review.CreatedAt).IsRequired();

        // Cascade on both: talent and employer account deletion hard-delete the
        // application / company rows, and the reviews about them must go with them.
        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(review => review.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<JobApplication>()
            .WithMany()
            .HasForeignKey(review => review.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // JobId deliberately has no FK: reviews belong to the company and must
        // outlive the job posting (closed, expired or deleted).
        builder.HasIndex(review => review.JobApplicationId).IsUnique();
        builder.HasIndex(review => new { review.CompanyId, review.Status });
        builder.HasIndex(review => review.TalentUserId);
        builder.HasIndex(review => new { review.Status, review.SubmittedAt });
    }
}
