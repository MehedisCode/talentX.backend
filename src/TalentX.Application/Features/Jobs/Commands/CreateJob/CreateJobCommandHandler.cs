using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Commands.CreateJob;

public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, Guid>
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateJobCommandHandler(
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to post a job.");

        var job = Job.Create(
            JobRules.NormalizeText(employerId),
            JobRules.NormalizeText(request.Title),
            JobRules.NormalizeText(request.CompanyName),
            JobRules.NormalizeText(request.Description),
            request.EmploymentType,
            request.ExperienceLevel,
            JobRules.NormalizeText(request.Location),
            request.WorkArrangement,
            JobRules.NormalizeSkills(request.RequiredSkills),
            request.Vacancies,
            request.ExpireAt
        );

        job.UpdateWizardDetails(
            request.City != null ? JobRules.NormalizeText(request.City) : null,
            request.Country != null ? JobRules.NormalizeText(request.Country) : null,
            request.MinOverlapHours,
            null
        );

        if (request.Publish)
        {
            var employer = await _unitOfWork.Employers.GetByUserIdAsync(employerId, cancellationToken)
                ?? throw new NotFoundException(nameof(Employer), employerId);

            if (employer.VerificationStatus != EmployerVerificationStatus.Verified)
                throw new ForbiddenException("Only verified employers can publish jobs. Your account is pending verification.");

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

        await _jobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return job.Id;
    }
}
