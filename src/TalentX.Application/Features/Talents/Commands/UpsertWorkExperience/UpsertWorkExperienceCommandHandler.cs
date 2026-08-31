using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Talents.Commands.UpsertWorkExperience;

public sealed class UpsertWorkExperienceCommandHandler : IRequestHandler<UpsertWorkExperienceCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpsertWorkExperienceCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(UpsertWorkExperienceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to manage your work experience.");

        if (request.Id is null)
        {
            var workExperience = new WorkExperience(
                userId,
                request.CompanyName,
                request.JobTitle,
                request.StartDate,
                request.EndDate,
                request.IsCurrent);

            await _unitOfWork.WorkExperiences.AddAsync(workExperience, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return workExperience.Id;
        }

        var existingWorkExperience = await _unitOfWork.WorkExperiences.GetByIdAsync(request.Id.Value, cancellationToken);
        if (existingWorkExperience is null || existingWorkExperience.UserId != userId)
            throw new NotFoundException($"Work experience '{request.Id}' was not found.");

        existingWorkExperience.Update(
            request.CompanyName,
            request.JobTitle,
            request.StartDate,
            request.EndDate,
            request.IsCurrent);

        _unitOfWork.WorkExperiences.Update(existingWorkExperience);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return existingWorkExperience.Id;
    }
}
