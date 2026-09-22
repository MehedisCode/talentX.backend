using AutoMapper;
using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Domain;

namespace TalentX.Application.Features.Jobs.Queries.GetPublishedJobs;

public class GetPublishedJobsQueryHandler : IRequestHandler<GetPublishedJobsQuery, PaginatedList<JobListItemDto>>
{
    private static readonly TimeSpan LogoUrlExpiry = TimeSpan.FromHours(1);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPresignedUrlService _presignedUrlService;

    public GetPublishedJobsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, IPresignedUrlService presignedUrlService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _presignedUrlService = presignedUrlService;
    }

    public async Task<PaginatedList<JobListItemDto>> Handle(GetPublishedJobsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var jobs = await _unitOfWork.Jobs.GetPublishedJobsAsync(
            page,
            pageSize,
            cancellationToken,
            keyword: request.Keyword,
            location: request.Location,
            employmentType: request.EmploymentType,
            experienceLevel: request.ExperienceLevel,
            workArrangement: request.WorkArrangement);

        var items = _mapper.Map<List<JobListItemDto>>(jobs.Items);

        // Company entity is the source of truth for name/logo.
        // Single batch lookup for the page; fall back to Job.CompanyName for legacy jobs.
        var jobsByIndex = jobs.Items.ToList();
        var employerIds = jobsByIndex
            .Select(j => j.EmployerId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        if (employerIds.Count > 0)
        {
            var companies = await _unitOfWork.Companies.GetByEmployerIdsAsync(employerIds, cancellationToken)
                ?? Enumerable.Empty<Company>();
            var companiesByEmployerId = companies.ToDictionary(c => c.EmployerId, c => c);

            for (var i = 0; i < items.Count && i < jobsByIndex.Count; i++)
            {
                if (!companiesByEmployerId.TryGetValue(jobsByIndex[i].EmployerId, out var company))
                {
                    continue;
                }

                items[i].CompanyId = company.Id;
                items[i].CompanyName = company.Name;
                if (!string.IsNullOrWhiteSpace(company.LogoStorageKey))
                {
                    items[i].LogoUrl = await _presignedUrlService.CreateDownloadUrlAsync(
                        company.LogoStorageKey, LogoUrlExpiry, cancellationToken);
                }
            }
        }

        return new PaginatedList<JobListItemDto>(items, jobs.Page, jobs.PageSize, jobs.TotalCount);
    }
}
