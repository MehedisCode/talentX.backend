using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Ai;

public static class ExtractionStartup
{
    public static IServiceCollection AddExtractionServices(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddHttpClient<IResumeExtractionService, GeminiExtractionService>();
        return services;
    }
}
