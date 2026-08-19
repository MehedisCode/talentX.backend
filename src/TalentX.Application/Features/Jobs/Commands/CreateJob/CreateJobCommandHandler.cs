using FluentValidation;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

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
        // var employerId = _currentUserService.UserId
        //     ?? throw new UnauthorizedException("You must be signed in to post a job.");
        var employerId = "0000000000010010";

        var job = Job.CreateDraft(
            employerId,
            request.Title,
            request.CompanyName,
            request.Description,
            request.EmploymentType,
            request.ExperienceLevel,
            request.Location,
            request.WorkArrangement,
            request.RequiredSkills,
            request.Vacancies,
            request.ExpireAt
        );

        if (request.Publish)
        {
            try
            {
                job.Publish();
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException(ex.Message);
            }
        }

        await _jobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return job.Id;
    }
}
