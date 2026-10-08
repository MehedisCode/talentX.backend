using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class InterviewInvitationConfiguration : IEntityTypeConfiguration<InterviewInvitation>
{
    public void Configure(EntityTypeBuilder<InterviewInvitation> builder)
    {
        builder.ToTable("InterviewInvitations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobId).IsRequired();
        builder.Property(x => x.JobApplicationId).IsRequired();
        builder.Property(x => x.EmployerUserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TalentUserId).HasMaxLength(200).IsRequired();

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.Property(x => x.AssessmentUrl).HasMaxLength(1000);
        builder.Property(x => x.LocationOrLink).HasMaxLength(1000);
        builder.Property(x => x.Instructions).HasMaxLength(4000);
        builder.Property(x => x.DeclineReason).HasMaxLength(2000);

        builder.Property(x => x.SentAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasMany(x => x.Timeslots)
            .WithOne()
            .HasForeignKey(t => t.InterviewInvitationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Job)
            .WithMany()
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.JobApplication)
            .WithMany()
            .HasForeignKey(x => x.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.JobId);
        builder.HasIndex(x => x.JobApplicationId);
        builder.HasIndex(x => x.EmployerUserId);
        builder.HasIndex(x => x.TalentUserId);
        builder.HasIndex(x => new { x.JobApplicationId, x.Status });

        builder.HasIndex(x => x.JobApplicationId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Pending'")
            .HasDatabaseName("IX_InterviewInvitations_JobApplicationId_Pending_Unique");
    }
}
