using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TalentX.Application.Common.Behaviours;

namespace TalentX.Application;

public static class Startup
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR handlers from the Application assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });

        // Register all FluentValidation validators from the Application assembly
        services.AddValidatorsFromAssembly(assembly);

        // Register AutoMapper profiles across assemblies
        services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));

        return services;
    }

    /// <summary>
    /// Backward-compatible alias for AddApplicationServices.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
        => services.AddApplicationServices();
}
