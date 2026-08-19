using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Jobs.Commands.UpdateJob;

public class UpdateJobCommandHandler : IRequestHandler<UpdateJobCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateJobCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // TODO: decide whether a Published job can still be edited via this route.
    public async Task Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.Jobs.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.Id}' was not found.");

        job.UpdateDetails(
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

        _unitOfWork.Jobs.Update(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
