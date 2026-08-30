using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.JobApplications.Queries.GetJobApplicationById;

public sealed class GetJobApplicationByIdQueryHandler
    : IRequestHandler<GetJobApplicationByIdQuery, JobApplicationDetailsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetJobApplicationByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<JobApplicationDetailsDto> Handle(
        GetJobApplicationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("Something went wrong. Cannot find user id.");

        var application = await _unitOfWork.JobApplications.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Application '{request.Id}' was not found.");

    
        if (application.TalentUserId != userId)
        {
            var job = await _unitOfWork.Jobs.GetByIdAsync(application.JobId, cancellationToken);
            if (job is null || job.EmployerId != userId)
                throw new ForbiddenException("You do not have permission to view this application.");
        }

        return new JobApplicationDetailsDto(
            application.Id,
            application.JobId,
            application.ApplicantName,
            application.ApplicantEmail,
            application.ResumeStorageKey,
            application.CoverLetter,
            application.ExpectedSalary,
            application.TotalYearsExperience,
            application.Location,
            application.Status.ToString(),
            application.AppliedAt,
            application.UpdatedAt);
    }
}
