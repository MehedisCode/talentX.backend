using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.RecipientUserId)
            .HasMaxLength(200)
            .IsRequired();

        // Custom FK: index only. Deliberately NO HasOne/WithMany — see DB_Schema.md section 0.
        builder.HasIndex(notification => notification.RecipientUserId);

        builder.Property(notification => notification.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(notification => notification.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(notification => notification.Body)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(notification => notification.Metadata)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(notification => notification.CreatedAt)
            .IsRequired();

        builder.HasIndex(notification => new { notification.RecipientUserId, notification.ReadAt })
            .HasDatabaseName("IX_Notifications_RecipientUserId_ReadAt");
    }
}
