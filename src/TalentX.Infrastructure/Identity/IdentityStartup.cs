using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Infrastructure.Identity.Models;
using TalentX.Infrastructure.Identity.Services;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Identity;

public static class IdentityStartup
{
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Identity
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // JWT settings
        var jwtSettings = GetJwtSettings(configuration);

        services.Configure<JwtSettings>(options =>
        {
            options.Secret = jwtSettings.Secret;
            options.Issuer = jwtSettings.Issuer;
            options.Audience = jwtSettings.Audience;
            options.AccessTokenMinutes = jwtSettings.AccessTokenMinutes;
            options.RefreshTokenExpiryDays = jwtSettings.RefreshTokenExpiryDays;
        });

        // Authentication - tokens arrive as a standard Authorization: Bearer header,
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

        services.AddHttpContextAccessor();
        services.AddAuthorization();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();

        return services;
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