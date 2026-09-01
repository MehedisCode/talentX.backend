using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.JobApplications.Commands.ApplyToJob;

public sealed class ApplyToJobCommandHandler
    : IRequestHandler<ApplyToJobCommand, ApplicationSubmittedDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ApplyToJobCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ApplicationSubmittedDto> Handle(
        ApplyToJobCommand request,
        CancellationToken cancellationToken)
    {
        var talentUserId = _currentUserService.UserId!;

        // Getting all the open jobs and published jobs
        var job = await _unitOfWork.Jobs.GetPublishedJobByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException("This job is no longer open for applications.");

        // check to prevent duplicate applications
        if (await _unitOfWork.JobApplications.ExistsForJobAndTalentAsync(job.Id, talentUserId, cancellationToken))
            throw new ConflictException("You have already applied to this job.");

        var application = new JobApplication(
            job.Id,
            talentUserId,
            JobApplicationRules.NormalizeRequired(request.ApplicantName),
            JobApplicationRules.NormalizeRequired(request.ApplicantEmail),
            JobApplicationRules.NormalizeRequired(request.ResumeStorageKey),
            JobApplicationRules.NormalizeOptional(request.CoverLetter),
            request.ExpectedSalary,
            request.TotalYearsExperience,
            JobApplicationRules.NormalizeOptional(request.Location));

        await _unitOfWork.JobApplications.AddAsync(application, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ApplicationSubmittedDto(
            application.Id,
            application.JobId,
            application.Status.ToString(),
            application.AppliedAt);
    }
}
