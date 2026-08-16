using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.EmployerId)
            .IsRequired();

        builder.HasIndex(j => j.EmployerId);

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.Description)
            .IsRequired()
            .HasMaxLength(10000);

        builder.Property(j => j.Location)
            .IsRequired()
            .HasMaxLength(200);

        // RequiredSkills list stored as a JSON or a separate table. Since EF Core 8 supports primitive collections,
        // we can store List<string> as JSON if we use HasConversion or simply it's supported depending on the provider.
        // For SQL Server in EF Core 8+, primitive collections are mapped to JSON arrays by default.
        // No explicit configuration needed for primitive collection unless needed.
    }
}
