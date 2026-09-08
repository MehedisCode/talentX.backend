using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class InterviewTimeslotConfiguration : IEntityTypeConfiguration<InterviewTimeslot>
{
    public void Configure(EntityTypeBuilder<InterviewTimeslot> builder)
    {
        builder.ToTable("InterviewTimeslots");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InterviewInvitationId).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();
        builder.Property(x => x.MeetingUrl).HasMaxLength(1000);
        builder.Property(x => x.IsBooked).IsRequired();
        builder.Property(x => x.BookedByTalentUserId).HasMaxLength(200);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.InterviewInvitationId);
        builder.HasIndex(x => x.IsBooked);
    }
}
