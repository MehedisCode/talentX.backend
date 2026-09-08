using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentX.Application.Common.Interfaces;
using TalentX.Infrastructure.Ai.Prompts;

namespace TalentX.Infrastructure.Ai;

public static class ExtractionStartup
{
    /// <summary>
    /// Registers AI services for resume extraction, job content generation, and talent matching.
    /// </summary>
    public static IServiceCollection AddExtractionServices(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        // Resume extraction service (existing)
        services.AddHttpClient<IResumeExtractionService, GeminiExtractionService>();

        // Job content generation service (new for multi-step wizard)
        services.AddHttpClient<IJobGenerationService, GeminiJobGenerationService>();

        // AI talent matching prompt factory & versions
        services.AddSingleton<ITalentMatchingPrompt, V1>();
        services.AddSingleton<ITalentMatchingPromptFactory, TalentMatchingPromptFactory>();

        // AI talent matching service
        services.AddHttpClient<ITalentMatchingService, GeminiTalentMatchingService>();

        return services;
    }
}
