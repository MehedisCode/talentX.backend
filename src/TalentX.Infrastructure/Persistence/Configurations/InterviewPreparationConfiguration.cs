using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class InterviewPreparationConfiguration
    : IEntityTypeConfiguration<InterviewPreparation>
{
    public void Configure(EntityTypeBuilder<InterviewPreparation> builder)
    {
        builder.ToTable("InterviewPreparations");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.InterviewInvitationId).IsRequired();
        builder.Property(p => p.TalentUserId).HasMaxLength(200).IsRequired();
        builder.Property(p => p.JobId).IsRequired();
        builder.Property(p => p.JobTitle).HasMaxLength(200).IsRequired();
        builder.Property(p => p.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.InterviewType).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Status).HasConversion<int>().IsRequired();

        // AI-generated JSON columns: unbounded `text` so an over-long model
        // output cannot fail the INSERT with a varchar overflow (500).
        builder.Property(p => p.TechnicalQuestions).IsRequired();
        builder.Property(p => p.BehavioralQuestions).IsRequired();
        builder.Property(p => p.ExperienceBasedQuestions).IsRequired();
        builder.Property(p => p.RoleSpecificQuestions).IsRequired();
        builder.Property(p => p.KeyTopicsToPrepare).IsRequired();
        builder.Property(p => p.SuggestedRevisionAreas).IsRequired();
        builder.Property(p => p.AdditionalQuestions);

        builder.HasIndex(p => p.TalentUserId);
        builder.HasIndex(p => p.InterviewInvitationId).IsUnique();
        builder.HasIndex(p => new { p.TalentUserId, p.JobId });

        builder.HasOne<InterviewInvitation>()
            .WithMany().HasForeignKey(p => p.InterviewInvitationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Job>()
            .WithMany().HasForeignKey(p => p.JobId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
