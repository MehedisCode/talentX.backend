using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class EmailDispatchLogConfiguration : IEntityTypeConfiguration<EmailDispatchLog>
{
    public void Configure(EntityTypeBuilder<EmailDispatchLog> builder)
    {
        builder.ToTable("EmailDispatchLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecipientEmail).IsRequired().HasMaxLength(256);
        builder.Property(x => x.RecipientName).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(512);
        builder.Property(x => x.HtmlBody).IsRequired();
        builder.Property(x => x.EmailType).IsRequired().HasMaxLength(100);

        builder.Property(x => x.Status).HasConversion<string>().IsRequired();

        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(x => new { x.Status, x.NextAttemptAt })
            .HasDatabaseName("IX_EmailDispatchLogs_Status_NextAttemptAt");
    }
}
