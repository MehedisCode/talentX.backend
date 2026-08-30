using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Commands.DeleteWorkExperience;

public sealed class DeleteWorkExperienceCommandHandler : IRequestHandler<DeleteWorkExperienceCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteWorkExperienceCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteWorkExperienceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to manage your work experience.");

        var workExperience = await _unitOfWork.WorkExperiences.GetByIdAsync(request.Id, cancellationToken);
        if (workExperience is null || workExperience.UserId != userId)
            throw new NotFoundException($"Work experience '{request.Id}' was not found.");

        _unitOfWork.WorkExperiences.Delete(workExperience);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
