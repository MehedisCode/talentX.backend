using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TalentX.Application.Common.Identity.Interfaces;
using TalentX.Application.Common.Interfaces;
using TalentX.Infrastructure.Configuration;
using TalentX.Infrastructure.Identity.Models;
using TalentX.Infrastructure.Identity.Services;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using TalentX.Infrastructure.Services;

namespace TalentX.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Database Connection (Granular DevOps environment variables or connection string fallback)
        var connectionString = GetDatabaseConnectionString(configuration);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var jwtSettings = GetJwtSettings(configuration);

        services.Configure<JwtSettings>(options =>
        {
            options.Secret = jwtSettings.Secret;
            options.Issuer = jwtSettings.Issuer;
            options.Audience = jwtSettings.Audience;
            options.AccessTokenMinutes = jwtSettings.AccessTokenMinutes;
            options.RefreshTokenExpiryDays = jwtSettings.RefreshTokenExpiryDays;
        });

        // Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Read the access token from the httpOnly cookie instead of the Authorization header
                    if (context.Request.Cookies.TryGetValue("TalentXAccessToken", out var token))
                    {
                        context.Token = token;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddHttpContextAccessor();

        // Configuration Options
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        // Application Services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IExternalIdentityService, ExternalIdentityService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddSingleton<IOtpService, OtpService>();
        services.AddScoped<IEmailService, EmailService>();
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

    /// <summary>
    /// Constructs JWT settings from environment variables (JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE),
    /// falling back to the "Jwt" configuration section in appsettings.json.
    /// </summary>
    private static JwtSettings GetJwtSettings(IConfiguration configuration)
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET")
                     ?? configuration["JWT_SECRET"]
                     ?? configuration["Jwt:Secret"]
                     ?? string.Empty;

        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                     ?? configuration["JWT_ISSUER"]
                     ?? configuration["Jwt:Issuer"]
                     ?? "TalentX";

        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                      ?? configuration["JWT_AUDIENCE"]
                      ?? configuration["Jwt:Audience"]
                      ?? "TalentX.Client";

        var accessMinutes = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_MINUTES")
                            ?? configuration["JWT_ACCESS_TOKEN_MINUTES"]
                            ?? configuration["Jwt:AccessTokenMinutes"];

        var refreshDays = Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRY_DAYS")
                          ?? configuration["JWT_REFRESH_TOKEN_EXPIRY_DAYS"]
                          ?? configuration["Jwt:RefreshTokenExpiryDays"];

        return new JwtSettings
        {
            Secret = secret,
            Issuer = issuer,
            Audience = audience,
            AccessTokenMinutes = int.TryParse(accessMinutes, out var mins) ? mins : 15,
            RefreshTokenExpiryDays = int.TryParse(refreshDays, out var days) ? days : 7
        };
    }
}