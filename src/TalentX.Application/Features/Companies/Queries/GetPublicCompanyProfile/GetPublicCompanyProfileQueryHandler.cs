using System.Linq;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Companies.DTOs;

namespace TalentX.Application.Features.Companies.Queries.GetPublicCompanyProfile;

public sealed class GetPublicCompanyProfileQueryHandler : IRequestHandler<GetPublicCompanyProfileQuery, PublicCompanyProfileDto>
{
    private static readonly TimeSpan LogoUrlExpiry = TimeSpan.FromHours(1);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPresignedUrlService _presignedUrlService;

    public GetPublicCompanyProfileQueryHandler(
        IUnitOfWork unitOfWork,
        IPresignedUrlService presignedUrlService)
    {
        _unitOfWork = unitOfWork;
        _presignedUrlService = presignedUrlService;
    }

    public async Task<PublicCompanyProfileDto> Handle(GetPublicCompanyProfileQuery request, CancellationToken cancellationToken)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(request.Id, cancellationToken);

        if(company is null)
        {
            company = await _unitOfWork.Companies.GetByEmployerIdAsync(request.Id.ToString(), cancellationToken)
            ?? throw new NotFoundException($"Company was not found.");
        }

        var activeJobs = await _unitOfWork.Jobs.GetActivePublishedJobsByEmployerIdAsync(company.EmployerId, cancellationToken);

        string? logoUrl = null;
        if (!string.IsNullOrWhiteSpace(company.LogoStorageKey))
        {
            logoUrl = await _presignedUrlService.CreateDownloadUrlAsync(company.LogoStorageKey, LogoUrlExpiry, cancellationToken);
        }

        var publishedJobDtos = activeJobs.Select(job => new PublicCompanyJobDto(
            job.Id,
            job.Title,
            job.Location,
            job.City,
            job.Country,
            job.EmploymentType,
            job.WorkArrangement,
            job.ExperienceLevel,
            job.Vacancies,
            job.ExpireAt,
            job.PublishedAt
        )).ToList();

        return new PublicCompanyProfileDto(
            company.Id,
            company.Name,
            logoUrl,
            company.Description,
            company.Industry,
            company.CompanySize,
            company.Website,
            company.Location,
            company.LinkedInUrl,
            company.TwitterUrl,
            company.FacebookUrl,
            publishedJobDtos.Count,
            publishedJobDtos
        );
    }
}
