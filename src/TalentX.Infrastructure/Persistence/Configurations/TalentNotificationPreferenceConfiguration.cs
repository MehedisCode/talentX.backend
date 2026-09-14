using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class TalentNotificationPreferenceConfiguration : IEntityTypeConfiguration<TalentNotificationPreference>
{
    public void Configure(EntityTypeBuilder<TalentNotificationPreference> builder)
    {
        builder.ToTable("TalentNotificationPreferences");
        builder.HasKey(preference => preference.Id);

        builder.Property(preference => preference.TalentId)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(preference => preference.TalentId)
            .IsUnique();

        builder.Property(preference => preference.JobAlertsEmailEnabled)
            .IsRequired();

        builder.Property(preference => preference.CreatedAt)
            .IsRequired();
    }
}
