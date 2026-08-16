using MediatR;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TalentX.Application.Common.Behaviours;
using AutoMapper;

namespace TalentX.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services
    )
    {
        services.AddMediatR(cfg =>
        {
            // Scan the assembly where THIS class (TalentX.Application.DependencyInjection) lives
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddAutoMapper(cfg => { }, typeof(DependencyInjection).Assembly);

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehaviour<,>)
        );

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(LoggingBehaviour<,>)
        );

        return services;
    }
}