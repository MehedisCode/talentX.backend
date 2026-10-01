using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(auditLog => auditLog.Id);

        builder.Property(auditLog => auditLog.Actor)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(auditLog => auditLog.Action)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(auditLog => auditLog.IsSystem)
            .IsRequired();

        builder.Property(auditLog => auditLog.IpAddress)
            .HasMaxLength(45);

        builder.HasIndex(auditLog => auditLog.CreatedAt);
        builder.HasIndex(auditLog => auditLog.Actor);
    }
}
