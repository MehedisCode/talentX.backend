using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using TalentX.Infrastructure.Identity.Seed;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.SeedData;

namespace TalentX.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations and seeds baseline data on startup. Pulled out of
/// Program.cs so Program.cs only has a single call site.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, IHostEnvironment environment)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!environment.IsDevelopment())
        {
            await db.Database.MigrateAsync();
            await IdentitySeeder.SeedAsync(services);
            return;
        }

        try
        {
            await db.Database.MigrateAsync();
            await SeedJobsAsync(db);
            await IdentitySeeder.SeedAsync(services);
        }
        catch (PostgresException ex) when (ex.SqlState is "42P07" or "42P01")
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
            logger.LogWarning("Outdated database schema detected ({SqlState}). Re-creating database...", ex.SqlState);

            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            await SeedJobsAsync(db);
            await IdentitySeeder.SeedAsync(services);
        }
    }

    private static async Task SeedJobsAsync(ApplicationDbContext db)
    {
        if (await db.Jobs.AnyAsync())
            return;

        db.Jobs.AddRange(JobSeedData.GetJobs());
        await db.SaveChangesAsync();
    }
}
