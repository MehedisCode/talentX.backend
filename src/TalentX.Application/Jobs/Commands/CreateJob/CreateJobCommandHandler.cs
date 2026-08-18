using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Domain.Entities;

namespace TalentX.Application.Jobs.Commands.CreateJob;

public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateJobCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // TODO: user id implementation remaining — resolve the authenticated employer via ICurrentUserService and pass it to Job.CreateDraft.
    public async Task<Guid> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        var job = Job.CreateDraft(
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

        await _unitOfWork.Jobs.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return job.Id;
    }
}
