using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations");
        builder.HasKey(invitation => invitation.Id);

        builder.Property(invitation => invitation.JobId)
            .IsRequired();

        builder.Property(invitation => invitation.TalentUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(invitation => invitation.InvitedDate)
            .IsRequired();

        builder.Property(invitation => invitation.IsActive)
            .IsRequired();

        builder.Property(invitation => invitation.CreatedAt)
            .IsRequired();

        builder.Property(invitation => invitation.UpdatedAt);

        builder.HasIndex(invitation => new { invitation.JobId, invitation.TalentUserId });
        builder.HasIndex(invitation => invitation.IsActive);

        builder.HasOne<Job>()
            .WithMany()
            .HasForeignKey(invitation => invitation.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
