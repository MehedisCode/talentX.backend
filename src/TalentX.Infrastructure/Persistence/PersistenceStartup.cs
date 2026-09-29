using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Interceptors;
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

        services.AddScoped<AuditLogSaveChangesInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly("TalentX.Migrations"))
                   .ConfigureWarnings(warningConfiguration => warningConfiguration.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                   .AddInterceptors(serviceProvider.GetRequiredService<AuditLogSaveChangesInterceptor>()));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
        services.AddScoped<IJobApplicationStatusHistoryRepository, JobApplicationStatusHistoryRepository>();
        services.AddScoped<IEmployerRepository, EmployerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITalentRepository, TalentRepository>();
        services.AddScoped<IResumeRepository, ResumeRepository>();
        services.AddScoped<IWorkExperienceRepository, WorkExperienceRepository>();
        services.AddScoped<IEducationRepository, EducationRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<ISavedJobRepository, SavedJobRepository>();
        services.AddScoped<IInterviewInvitationRepository, InterviewInvitationRepository>();
        services.AddScoped<IEmailDispatchLogRepository, EmailDispatchLogRepository>();
        services.AddScoped<IJobTalentMatchRepository, JobTalentMatchRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IJobAlertRepository, JobAlertRepository>();
        services.AddScoped<ITalentNotificationPreferenceRepository, TalentNotificationPreferenceRepository>();
        services.AddScoped<IJobAlertNotificationHistoryRepository, JobAlertNotificationHistoryRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IJobModerationHistoryRepository, JobModerationHistoryRepository>();
        services.AddScoped<ICompanyReviewRepository, CompanyReviewRepository>();
        services.AddScoped<IInterviewPreparationConfigRepository, InterviewPreparationConfigRepository>();
        services.AddScoped<IInterviewPreparationConfigProvider, Infrastructure.Services.InterviewPreparationConfigProvider>();

        return services;
    }

    // Individual DevOps environment variables (DB_HOST, DB_PORT, DB_NAME, DB_USER_NAME, DB_PASSWORD)
    // take precedence over ConnectionStrings:DefaultConnection.
    private static string GetDatabaseConnectionString(IConfiguration configuration)
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? configuration["DB_HOST"];
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? configuration["DB_PORT"] ?? "5432";
        var database = Environment.GetEnvironmentVariable("DB_NAME") ?? configuration["DB_NAME"];
        var username = Environment.GetEnvironmentVariable("DB_USER_NAME")
                       ?? Environment.GetEnvironmentVariable("DB_USERNAME")
                       ?? Environment.GetEnvironmentVariable("DB_USER")
                       ?? configuration["DB_USER_NAME"]
                       ?? configuration["DB_USERNAME"];
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? configuration["DB_PASSWORD"];

        if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(database))
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = int.TryParse(port, out var parsedPort) ? parsedPort : 5432,
                Database = database,
                Username = username ?? "postgres",
                Password = password ?? string.Empty,
                IncludeErrorDetail = true
            };

            return builder.ConnectionString;
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
