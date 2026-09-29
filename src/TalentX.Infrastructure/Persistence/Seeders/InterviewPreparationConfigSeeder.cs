using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TalentX.Application.Features.InterviewPreparation;
using TalentX.Domain;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Seeders;

public static class InterviewPreparationConfigSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<InterviewPreparationSettings>>().Value;

        var exists = await context.InterviewPreparationConfigs.AnyAsync();
        if (exists) return;

        var config = InterviewPreparationConfig.CreateDefault(
            settings.DailyGenerateLimit,
            settings.DailyGenerationLimit,
            settings.MaxRegenerationCount,
            settings.AccessWindowAfterInterviewMinutes);

        await context.InterviewPreparationConfigs.AddAsync(config);
        await context.SaveChangesAsync();
    }
}
