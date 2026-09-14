using System.Collections.Generic;
using System.Linq;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Companies.DTOs;

namespace TalentX.Application.Features.Companies.Queries.GetCompanyPublishedJobs;

public sealed class GetCompanyPublishedJobsQueryHandler : IRequestHandler<GetCompanyPublishedJobsQuery, IReadOnlyList<PublicCompanyJobDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCompanyPublishedJobsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PublicCompanyJobDto>> Handle(GetCompanyPublishedJobsQuery request, CancellationToken cancellationToken)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new NotFoundException($"Company with ID '{request.CompanyId}' was not found.");

        var activeJobs = await _unitOfWork.Jobs.GetActivePublishedJobsByEmployerIdAsync(company.EmployerId, cancellationToken);

        return activeJobs.Select(job => new PublicCompanyJobDto(
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
    }
}
