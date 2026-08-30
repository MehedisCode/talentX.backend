using FluentValidation;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Jobs.Commands.UpdateJob;

public class UpdateJobCommandHandler : IRequestHandler<UpdateJobCommand>
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateJobCommandHandler(
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    
    public async Task Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to edit a job.");

        var job = await _jobRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.Id}' was not found.");

        if (job.EmployerId != employerId)
            throw new ForbiddenException("You do not have permission to edit this job.");

        job.SetDetails(
            request.Title != null ? JobRules.NormalizeText(request.Title) : job.Title,
            request.CompanyName != null ? JobRules.NormalizeText(request.CompanyName) : job.CompanyName,
            request.Description != null ? JobRules.NormalizeText(request.Description) : job.Description,
            request.EmploymentType ?? job.EmploymentType,
            request.ExperienceLevel ?? job.ExperienceLevel,
            request.Location != null ? JobRules.NormalizeText(request.Location) : job.Location,
            request.WorkArrangement ?? job.WorkArrangement,
            request.RequiredSkills != null ? JobRules.NormalizeSkills(request.RequiredSkills) : job.RequiredSkills,
            request.Vacancies ?? job.Vacancies,
            request.ExpireAt ?? job.ExpireAt
        );

        job.UpdateWizardDetails(
            request.City != null ? JobRules.NormalizeText(request.City) : job.City,
            request.Country != null ? JobRules.NormalizeText(request.Country) : job.Country,
            request.MinOverlapHours ?? job.MinOverlapHours,
            job.Responsibilities
        );

        if (request.Publish)
        {
            try
            {
                JobRules.EnsurePublishable(job);
                job.MarkAsPublished();
            }
            catch (InvalidOperationException ex)
            {
                throw new FluentValidation.ValidationException(ex.Message);
            }
        }

        _jobRepository.Update(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
