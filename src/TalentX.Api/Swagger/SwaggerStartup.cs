
using Microsoft.OpenApi;

namespace TalentX.API.Swagger;

public static class SwaggerStartup
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "TalentX", Version = "v1" });

            options.UseInlineDefinitionsForEnums();

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the access token here (no 'Bearer ' prefix needed)."
            });

            options.OperationFilter<AuthorizeCheckOperationFilter>();
        });

        return services;
    }
}