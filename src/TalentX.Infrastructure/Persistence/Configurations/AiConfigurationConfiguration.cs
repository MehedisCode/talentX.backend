using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class AiConfigurationConfiguration : IEntityTypeConfiguration<AiConfiguration>
{
    public void Configure(EntityTypeBuilder<AiConfiguration> builder)
    {
        builder.ToTable("AiConfigurations");

        builder.HasKey(configuration => configuration.Id);

        builder.Property(configuration => configuration.ModelName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(configuration => configuration.EncryptedApiKey)
            .HasMaxLength(1000)
            .IsRequired(false);
    }
}
