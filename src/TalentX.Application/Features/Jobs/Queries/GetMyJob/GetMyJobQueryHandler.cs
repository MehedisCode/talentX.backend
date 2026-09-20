using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Jobs.Queries.GetMyJob;

public class GetMyJobQueryHandler : IRequestHandler<GetMyJobQuery, MyJobDto>
{
    private readonly IJobRepository _jobRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMyJobQueryHandler(IJobRepository jobRepository, ICurrentUserService currentUserService)
    {
        _jobRepository = jobRepository;
        _currentUserService = currentUserService;
    }

    public async Task<MyJobDto> Handle(GetMyJobQuery request, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view this job.");

        var job = await _jobRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.Id}' was not found.");

        if (job.EmployerId != employerId)
            throw new ForbiddenException("You do not have permission to view this job.");

        return new MyJobDto
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
            Status = job.GetEffectiveStatus().ToString(),
            Vacancies = job.Vacancies,
            ExpireAt = job.ExpireAt,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            City = job.City,
            Country = job.Country,
            MinOverlapHours = job.MinOverlapHours,
            SalaryType = job.SalaryType,
            SalaryMin = job.SalaryMin,
            SalaryMax = job.SalaryMax,
            SalaryCurrency = job.SalaryCurrency,
            SalaryPeriod = job.SalaryPeriod,
            Requirements = job.Requirements
        };
    }
}
