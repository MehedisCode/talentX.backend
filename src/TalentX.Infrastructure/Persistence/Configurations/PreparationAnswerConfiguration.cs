using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class PreparationAnswerConfiguration
    : IEntityTypeConfiguration<PreparationAnswer>
{
    public void Configure(EntityTypeBuilder<PreparationAnswer> builder)
    {
        builder.ToTable("PreparationAnswers");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.InterviewPreparationId).IsRequired();
        builder.Property(a => a.Category).HasMaxLength(50).IsRequired();
        builder.Property(a => a.QuestionIndex).IsRequired();
        builder.Property(a => a.QuestionText).HasMaxLength(2000).IsRequired();
        builder.Property(a => a.AnswerText).HasMaxLength(4000).IsRequired();
        builder.Property(a => a.IsPracticed).IsRequired();

        builder.HasIndex(a => new { a.InterviewPreparationId, a.Category, a.QuestionIndex })
            .IsUnique();

        builder.HasOne<InterviewPreparation>()
            .WithMany().HasForeignKey(a => a.InterviewPreparationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
