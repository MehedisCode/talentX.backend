using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class InterviewPreparationEvaluationConfiguration
    : IEntityTypeConfiguration<InterviewPreparationEvaluation>
{
    public void Configure(EntityTypeBuilder<InterviewPreparationEvaluation> builder)
    {
        builder.ToTable("InterviewPreparationEvaluations");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.InterviewPreparationId).IsRequired();
        builder.Property(e => e.OverallScore).IsRequired();
        builder.Property(e => e.Summary).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.Strengths).HasMaxLength(8000).IsRequired();
        builder.Property(e => e.AreasToImprove).HasMaxLength(8000).IsRequired();
        builder.Property(e => e.QuestionFeedback).IsRequired();
        builder.Property(e => e.AnsweredCount).IsRequired();
        builder.Property(e => e.TotalCount).IsRequired();
        builder.Property(e => e.EvaluatedAt).IsRequired();

        builder.HasIndex(e => e.InterviewPreparationId).IsUnique();

        builder.HasOne<InterviewPreparation>()
            .WithMany().HasForeignKey(e => e.InterviewPreparationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
