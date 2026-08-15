using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using TalentX.Api.Extensions;
using TalentX.Application;
using TalentX.Infrastructure;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var frontendUrl = builder.Configuration["Cors:FrontendUrl"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

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
    });


builder.Services.AddSwaggerGen();


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGlobalExceptionMiddleware();

app.UseRequestLoggingMiddleware();

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// Development convenience only — production migrations run via the deployment
// pipeline, not app startup.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TalentX.Infrastructure.Persistence.Contexts.ApplicationDbContext>();
    db.Database.Migrate();
    if (!db.Jobs.Any())
    {
        db.Jobs.AddRange(TalentX.Infrastructure.Persistence.SeedData.JobSeedData.GetJobs());
        db.SaveChanges();
    }
}

app.Run();