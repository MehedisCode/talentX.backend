using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TalentX.Application.Common.Identity.Interfaces;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Infrastructure.Auth;
using TalentX.Infrastructure.Common;
using TalentX.Infrastructure.Identity.Models;
using TalentX.Infrastructure.Identity.Services;
using TalentX.Infrastructure.Mailing;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;

namespace TalentX.Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Database Connection
        var connectionString = GetDatabaseConnectionString(configuration);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly("TalentX.Migrations.PostgreSQL")));

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
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IExternalIdentityService, ExternalIdentityService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmployerRepository, EmployerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IOtpService, OtpService>();
        services.AddSingleton<IEmailService, EmailService>();

        return services;
    }

    /// <summary>
    /// Backward-compatible alias for AddInfrastructureServices.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    ) => services.AddInfrastructureServices(configuration);

    private static string GetDatabaseConnectionString(IConfiguration configuration)
    {
        // 1. Try granular DevOps environment variables first (matching .env standard)
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? configuration["DB_HOST"];
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? configuration["DB_PORT"] ?? "5432";
        var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? configuration["DB_NAME"];
        var dbUser = Environment.GetEnvironmentVariable("DB_USER_NAME") ?? configuration["DB_USER_NAME"];
        var dbPass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? configuration["DB_PASSWORD"];

        if (!string.IsNullOrWhiteSpace(dbHost) &&
            !string.IsNullOrWhiteSpace(dbName) &&
            !string.IsNullOrWhiteSpace(dbUser) &&
            !string.IsNullOrWhiteSpace(dbPass))
        {
            return $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass};Include Error Detail=true";
        }

        // 2. Fall back to standard ConnectionStrings:DefaultConnection
        var connStr = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connStr))
        {
            return connStr;
        }

        // 3. Sensible local developer default
        return "Host=localhost;Port=5432;Database=talentx;Username=postgres;Password=postgres;Include Error Detail=true";
    }

    private static JwtSettings GetJwtSettings(IConfiguration configuration)
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET")
                     ?? configuration["JWT_SECRET"]
                     ?? configuration["Jwt:Secret"]
                     ?? "TalentXDefaultSecretKey123456789012345678901234567890";

        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                     ?? configuration["JWT_ISSUER"]
                     ?? configuration["Jwt:Issuer"]
                     ?? "TalentX";

        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                       ?? configuration["JWT_AUDIENCE"]
                       ?? configuration["Jwt:Audience"]
                       ?? "TalentX.Client";

        return new JwtSettings
        {
            Secret = secret,
            Issuer = issuer,
            Audience = audience,
            AccessTokenMinutes = configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 15,
            RefreshTokenExpiryDays = configuration.GetValue<int?>("Jwt:RefreshTokenExpiryDays") ?? 7
        };
    }
}
