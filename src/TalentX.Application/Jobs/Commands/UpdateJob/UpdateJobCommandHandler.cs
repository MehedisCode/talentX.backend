using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Jobs.Commands.UpdateJob;

public class UpdateJobCommandHandler : IRequestHandler<UpdateJobCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateJobCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    
    public async Task Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.Jobs.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.Id}' was not found.");

        if (job.EmployerId != _currentUserService.UserId)
            throw new ForbiddenException();

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
