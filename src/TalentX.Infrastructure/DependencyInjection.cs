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
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
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

        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>()
            ?? new JwtSettings
            {
                Secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "MbBQkjWEwRtON2jZs6J57djfewfGWiPHZ2hFKFKZhr0SberrsfcrQ6wrWTCpYCSQjLNKMi9wI5I6NApdB4ymI2T",
                Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "talentx-api",
                Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "talentx-client",
                AccessTokenMinutes = 15,
                RefreshTokenExpiryDays = 7
            };

        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));

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
}