using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;

namespace TalentX.Infrastructure.Persistence;

public static class PersistenceStartup
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = GetDatabaseConnectionString(configuration);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly("TalentX.Migrations")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmployerRepository, EmployerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }

    /// <summary>
    /// Constructs a PostgreSQL connection string from individual DevOps environment variables
    /// (DB_HOST, DB_PORT, DB_NAME, DB_USER_NAME, DB_PASSWORD), falling back to ConnectionStrings:DefaultConnection.
    /// </summary>
    private static string GetDatabaseConnectionString(IConfiguration configuration)
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? configuration["DB_HOST"];
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? configuration["DB_PORT"] ?? "5432";
        var database = Environment.GetEnvironmentVariable("DB_NAME") ?? configuration["DB_NAME"];
        var username = Environment.GetEnvironmentVariable("DB_USER_NAME")
                       ?? Environment.GetEnvironmentVariable("DB_USERNAME")
                       ?? Environment.GetEnvironmentVariable("DB_USER")
                       ?? configuration["DB_USER_NAME"];
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? configuration["DB_PASSWORD"];

        if (!string.IsNullOrWhiteSpace(host) &&
            !string.IsNullOrWhiteSpace(database) &&
            !string.IsNullOrWhiteSpace(username) &&
            !string.IsNullOrWhiteSpace(password))
        {
            return $"Host={host};Port={port};Database={database};Username={username};Password={password};Include Error Detail=true";
        }

        var connStr = configuration.GetConnectionString("DefaultConnection")
                   ?? Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrWhiteSpace(connStr))
        {
            return connStr;
        }

        return "Host=localhost;Port=5432;Database=talentx;Username=postgres;Password=postgres;Include Error Detail=true";
    }
}
