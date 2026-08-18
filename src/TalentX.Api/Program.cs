using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using TalentX.Application;
using TalentX.Infrastructure;
using TalentX.Infrastructure.Identity.Seed;
using TalentX.Infrastructure.Middleware;

// Load .env from the backend root directory (talentx2026q3.backend/.env)
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

// Dynamic Port / Hosting URL Configuration (Support PORT or ASPNETCORE_URLS from DevOps .env)
var customPort = Environment.GetEnvironmentVariable("PORT") ?? builder.Configuration["PORT"];
if (!string.IsNullOrWhiteSpace(customPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{customPort}");
}

// Dynamic CORS Policy (Support comma-separated origins from CORS_ALLOWED_ORIGINS in .env)
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

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Converts all enums to string values during JSON serialization/deserialization
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        );
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Describes enums as strings in Swagger UI
    options.UseInlineDefinitionsForEnums();
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TalentX API", Version = "v1" });
});

var app = builder.Build();

await IdentitySeeder.SeedAsync(app.Services);

// Swagger enabled in Development or explicitly via ENABLE_SWAGGER=true in .env
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

app.UseGlobalExceptionMiddleware();
app.UseRequestLoggingMiddleware();
app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Development convenience only — production migrations run via the deployment pipeline
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TalentX.Infrastructure.Persistence.Contexts.ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();