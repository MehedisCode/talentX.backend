using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class JobTalentMatchConfiguration : IEntityTypeConfiguration<JobTalentMatch>
{
    public void Configure(EntityTypeBuilder<JobTalentMatch> builder)
    {
        builder.ToTable("JobTalentMatches");
        builder.HasKey(match => match.Id);

        builder.Property(match => match.JobId)
            .IsRequired();

        builder.Property(match => match.TalentUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(match => match.Score)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(match => match.CriteriaSnapshot)
            .HasMaxLength(4000);

        builder.Property(match => match.MatchedAt)
            .IsRequired();

        builder.Property(match => match.Status)
            .IsRequired()
            .HasDefaultValue(MatchStatus.Active);

        builder.Property(match => match.SkillBreakdown)
            .HasMaxLength(8000)
            .IsRequired(false);

        builder.Property(match => match.LastUpdatedAt);

        builder.Property(match => match.CreatedAt)
            .IsRequired();

        builder.Property(match => match.UpdatedAt);

        builder.HasIndex(match => match.JobId);

        builder.HasIndex(match => new { match.JobId, match.TalentUserId }, unique: true);

        builder.HasOne<Job>()
            .WithMany()
            .HasForeignKey(match => match.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}