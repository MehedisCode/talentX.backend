using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentX.Application.Common.Interfaces;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;

namespace TalentX.Infrastructure.Persistence;

public static class PersistenceStartup
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Connection (Granular DevOps environment variables or connection string fallback)
        var connectionString = GetDatabaseConnectionString(configuration);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly("TalentX.Migrations")));


        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    

    /// <summary>
    /// Constructs a PostgreSQL connection string from individual DevOps environment variables
    /// (DB_HOST, DB_PORT, DB_NAME, DB_USER_NAME, DB_PASSWORD), falling back to ConnectionStrings:DefaultConnection.
    /// </summary>
    private static string GetDatabaseConnectionString(IConfiguration configuration)
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? configuration["DB_HOST"];
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? configuration["DB_PORT"];
        var database = Environment.GetEnvironmentVariable("DB_NAME") ?? configuration["DB_NAME"];
        var username = Environment.GetEnvironmentVariable("DB_USER_NAME")
                       ?? Environment.GetEnvironmentVariable("DB_USERNAME")
                       ?? Environment.GetEnvironmentVariable("DB_USER")
                       ?? configuration["DB_USER_NAME"];
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? configuration["DB_PASSWORD"];

        if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(database))
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = int.TryParse(port, out var parsedPort) ? parsedPort : 5432,
                Database = database,
                Username = username ?? "postgres",
                Password = password ?? string.Empty
            };

            return builder.ConnectionString;
        }

        return configuration.GetConnectionString("DefaultConnection")
               ?? Environment.GetEnvironmentVariable("DATABASE_URL")
               ?? string.Empty;
    }
}