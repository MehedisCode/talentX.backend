using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class InterviewPreparationGenerationConfiguration
    : IEntityTypeConfiguration<InterviewPreparationGeneration>
{
    public void Configure(EntityTypeBuilder<InterviewPreparationGeneration> builder)
    {
        builder.ToTable("InterviewPreparationGenerations");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.InterviewPreparationId).IsRequired();
        builder.Property(g => g.TalentUserId).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Kind).HasConversion<int>().IsRequired();

        builder.HasIndex(g => g.TalentUserId);
        builder.HasIndex(g => new { g.TalentUserId, g.CreatedAt });
        builder.HasIndex(g => g.InterviewPreparationId);

        builder.HasOne<InterviewPreparation>()
            .WithMany().HasForeignKey(g => g.InterviewPreparationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
