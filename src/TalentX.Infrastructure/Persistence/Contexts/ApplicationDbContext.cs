using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Identity.Models;

namespace TalentX.Infrastructure.Persistence.Contexts;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<User> DomainUsers => Set<User>();
    public DbSet<Employer> Employers => Set<Employer>();
    public DbSet<Job> Jobs => Set<Job>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<Job>(entity =>
        {
            entity.Property(j => j.CompanyName).HasMaxLength(200).IsRequired();
            entity.Property(j => j.Title).HasMaxLength(200).IsRequired();
            entity.Property(j => j.City).HasMaxLength(100);
            entity.Property(j => j.Country).HasMaxLength(100);

            entity.Property(j => j.RoleType).HasConversion<string>();
            entity.Property(j => j.ExperienceLevel).HasConversion<string>();
            entity.Property(j => j.LocationPref).HasConversion<string>();
            entity.Property(j => j.Status).HasConversion<string>().IsRequired();

            entity.Property(j => j.SkillRequirements)
                .HasField("_skillRequirements")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

            entity.HasIndex(j => j.Status);
            entity.HasIndex(j => j.ExpiresAt);
            entity.HasIndex(j => new { j.Status, j.CreatedAt });
        });
    }
}
