using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.JobId)
            .IsRequired();

        builder.Property(i => i.TalentUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.InvitedDate)
            .IsRequired();

        builder.Property(i => i.IsActive)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.HasOne<Job>()
            .WithMany()
            .HasForeignKey(i => i.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.JobId);
        builder.HasIndex(i => i.TalentUserId);
        builder.HasIndex(i => new { i.JobId, i.TalentUserId });
    }
}
