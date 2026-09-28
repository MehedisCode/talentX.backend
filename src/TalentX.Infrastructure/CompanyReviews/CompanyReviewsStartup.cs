using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.CompanyReviews;

public static class CompanyReviewsStartup
{
    public static IServiceCollection AddCompanyReviews(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ICompanyReviewSettings>(CompanyReviewSettings.FromConfiguration(configuration));

        return services;
    }
}
