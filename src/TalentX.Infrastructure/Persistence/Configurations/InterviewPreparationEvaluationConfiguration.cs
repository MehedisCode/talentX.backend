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
        // Model output: unbounded `text` (see InterviewPreparationConfiguration).
        builder.Property(e => e.Summary).IsRequired();
        builder.Property(e => e.Strengths).IsRequired();
        builder.Property(e => e.AreasToImprove).IsRequired();
        builder.Property(e => e.QuestionFeedback).IsRequired();
        builder.Property(e => e.AnsweredCount).IsRequired();
        builder.Property(e => e.TotalCount).IsRequired();
        builder.Property(e => e.EvaluatedAt).IsRequired();
        builder.Property(e => e.ModelName).HasMaxLength(100);
        builder.Property(e => e.PromptVersion).HasMaxLength(50);
        builder.Property(e => e.LatencyMs);
        builder.Property(e => e.InputTokenCount);
        builder.Property(e => e.OutputTokenCount);

        builder.HasIndex(e => e.InterviewPreparationId).IsUnique();

        builder.HasOne<InterviewPreparation>()
            .WithMany().HasForeignKey(e => e.InterviewPreparationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
