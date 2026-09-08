using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Infrastructure.Auth;
using TalentX.Infrastructure.Common;
using TalentX.Infrastructure.Identity.Models;
using TalentX.Infrastructure.Identity.Services;
using TalentX.Infrastructure.Mailing;
using TalentX.Infrastructure.Mappings;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Identity;

public static class IdentityStartup
{
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
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

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromMinutes(15);
        });

        var jwtSettings = GetJwtSettings(configuration);

        services.Configure<JwtSettings>(options =>
        {
            options.Secret = jwtSettings.Secret;
            options.Issuer = jwtSettings.Issuer;
            options.Audience = jwtSettings.Audience;
            options.AccessTokenMinutes = jwtSettings.AccessTokenMinutes;
            options.RefreshTokenExpiryDays = jwtSettings.RefreshTokenExpiryDays;
        });

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

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.TryGetValue("TalentXAccessToken", out var token))
                    {
                        context.Token = token;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddHttpContextAccessor();
        services.AddAuthorization();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(EmployerMappingProfile).Assembly));

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IExternalIdentityService, ExternalIdentityService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddSingleton<IOtpService, OtpService>();
        services.AddSingleton<IEmailService, EmailService>();

        services.AddHostedService<EmailDispatchBackgroundWorker>();
        services.AddHostedService<GmailBounceListenerWorker>();

        return services;
    }

    // Environment variables (JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE) take precedence over
    // the "Jwt" configuration section in appsettings.json.
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

        var accessMinutes = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_MINUTES")
                            ?? Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES")
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
