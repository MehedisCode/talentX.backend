using Microsoft.EntityFrameworkCore;
using TalentX.API.Swagger;
using TalentX.Application;
using TalentX.Infrastructure;
using TalentX.Infrastructure.Identity.Seed;
using TalentX.Infrastructure.Middlewares;
using TalentX.Infrastructure.Persistence.Contexts;

// 1. Walk up directory tree to discover and load .env from solution root (talentx2026q3.backend/.env)
var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
var depth = 0;
while (!File.Exists(envFile) && depth++ < 5)
{
    envFile = Path.Combine(Path.GetDirectoryName(envFile)!, "..", ".env");
    envFile = Path.GetFullPath(envFile);
}

if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#')) continue;
        var idx = trimmed.IndexOf('=');
        if (idx <= 0) continue;
        var key = trimmed[..idx].Trim();
        var value = trimmed[(idx + 1)..].Trim().Trim('"').Trim('\'');
        Environment.SetEnvironmentVariable(key, value);
    }
}

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// 2. Dynamic Port / Hosting URL Configuration (Support PORT or ASPNETCORE_URLS from DevOps .env)
var customPort = Environment.GetEnvironmentVariable("PORT") ?? builder.Configuration["PORT"];
if (!string.IsNullOrWhiteSpace(customPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{customPort}");
}

// 3. Dynamic CORS Policy (Support comma-separated origins from CORS_ALLOWED_ORIGINS in .env)
const string CorsPolicyName = "AllowFrontend";
var rawCors = builder.Configuration["CORS_ALLOWED_ORIGINS"]
           ?? builder.Configuration["Cors:AllowedOrigins"]
           ?? builder.Configuration["Cors:FrontendUrl"]
           ?? "http://localhost:3000,http://127.0.0.1:3000";

var allowedOrigins = rawCors
    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Distinct()
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 4. Modular Service Registrations
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Converts all enums to string values during JSON serialization/deserialization
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        );
    });

var app = builder.Build();

// 5. Safe Database Migration & Identity Seeding (Runs migrations first, then seeds)
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    if (app.Environment.IsDevelopment())
    {
        await db.Database.MigrateAsync();
    }

    await IdentitySeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Database migration or seeding skipped or failed during startup: {Message}", ex.Message);
}

// 6. Swagger (Development or explicit ENABLE_SWAGGER=true in .env)
var enableSwagger = app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("ENABLE_SWAGGER");
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 7. Middlewares & Routing
app.UseInfrastructureMiddlewares();
app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }