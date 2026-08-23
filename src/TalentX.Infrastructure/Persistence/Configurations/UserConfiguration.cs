using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("DomainUsers");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.IsVerified)
            .HasDefaultValue(false)
            .IsRequired();
    }
}
