using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Talents.Commands.UpsertProject;

public sealed class UpsertProjectCommandHandler : IRequestHandler<UpsertProjectCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpsertProjectCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(UpsertProjectCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to manage your projects.");

        if (request.Id is null)
        {
            var project = new Project(
                userId,
                request.ProjectName,
                request.Role,
                request.ProjectType,
                request.StartDate,
                request.EndDate,
                request.IsOngoing,
                request.Description,
                request.TechnologiesUsed,
                request.ProjectUrl);

            await _unitOfWork.Projects.AddAsync(project, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return project.Id;
        }

        var existingProject = await _unitOfWork.Projects.GetByIdAsync(request.Id.Value, cancellationToken);
        if (existingProject is null || existingProject.UserId != userId)
        {
            throw new NotFoundException($"Project '{request.Id}' was not found.");
        }

        existingProject.Update(
            request.ProjectName,
            request.Role,
            request.ProjectType,
            request.StartDate,
            request.EndDate,
            request.IsOngoing,
            request.Description,
            request.TechnologiesUsed,
            request.ProjectUrl);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return existingProject.Id;
    }
}
