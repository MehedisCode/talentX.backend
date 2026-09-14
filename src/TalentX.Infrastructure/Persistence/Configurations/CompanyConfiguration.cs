using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain;

namespace TalentX.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.EmployerId)
            .IsRequired();

        builder.HasIndex(c => c.EmployerId)
            .IsUnique();

        builder.Property(c => c.Name)
            .IsRequired();

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.Property(c => c.LogoStorageKey);

        builder.Property(c => c.Description);

        builder.Property(c => c.Industry);

        builder.Property(c => c.CompanySize);

        builder.Property(c => c.Website);

        builder.Property(c => c.Location);

        builder.Property(c => c.LinkedInUrl);

        builder.Property(c => c.TwitterUrl);

        builder.Property(c => c.FacebookUrl);

    }
}
