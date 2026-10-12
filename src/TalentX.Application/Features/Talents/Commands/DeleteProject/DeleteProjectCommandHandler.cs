using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Commands.DeleteProject;

public sealed class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteProjectCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to manage your projects.");

        var project = await _unitOfWork.Projects.GetByIdAsync(request.Id, cancellationToken);
        if (project is null || project.UserId != userId)
        {
            throw new NotFoundException($"Project '{request.Id}' was not found.");
        }

        _unitOfWork.Projects.Delete(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
