using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentX.Infrastructure.Ai;
using TalentX.Infrastructure.FileStorage;
using TalentX.Infrastructure.Identity;
using TalentX.Infrastructure.Persistence;

namespace TalentX.Infrastructure;

public static class InfrastructureStartup
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddPersistence(configuration);
        services.AddIdentityServices(configuration);
        services.AddFileStorage(configuration);
        services.AddExtractionServices(configuration);

        return services;
    }

    // Backward-compatible alias for AddInfrastructure.
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    ) => services.AddInfrastructure(configuration);
}
