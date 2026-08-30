using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Jobs.Queries.GetMyJobs;

public class GetMyJobsQueryHandler : IRequestHandler<GetMyJobsQuery, List<MyJobDto>>
{
    private readonly IJobRepository _jobRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMyJobsQueryHandler(IJobRepository jobRepository, ICurrentUserService currentUserService)
    {
        _jobRepository = jobRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<MyJobDto>> Handle(GetMyJobsQuery request, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view your jobs.");

        var jobs = await _jobRepository.GetByEmployerIdAsync(employerId, cancellationToken);

        return jobs.Select(job => new MyJobDto
        {
            Id = job.Id,
            EmployerId = job.EmployerId,
            Title = job.Title,
            CompanyName = job.CompanyName,
            Description = job.Description,
            EmploymentType = job.EmploymentType.ToString(),
            ExperienceLevel = job.ExperienceLevel.ToString(),
            Location = job.Location,
            WorkArrangement = job.WorkArrangement.ToString(),
            RequiredSkills = job.RequiredSkills,
            Status = job.Status.ToString(),
            Vacancies = job.Vacancies,
            ExpireAt = job.ExpireAt,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            City = job.City,
            Country = job.Country,
            RequiredTimezoneOverlap = job.RequiredTimezoneOverlap
        }).ToList();
    }
}
