using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Talents.Commands.UpsertEducation;

public sealed class UpsertEducationCommandHandler : IRequestHandler<UpsertEducationCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpsertEducationCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(UpsertEducationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to manage your education.");

        if (request.Id is null)
        {
            var education = new Education(
                userId,
                request.Institution,
                request.Degree,
                request.StartDate,
                request.EndDate);

            await _unitOfWork.Educations.AddAsync(education, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return education.Id;
        }

        var existingEducation = await _unitOfWork.Educations.GetByIdAsync(request.Id.Value, cancellationToken);
        if (existingEducation is null || existingEducation.UserId != userId)
            throw new NotFoundException($"Education '{request.Id}' was not found.");

        existingEducation.Update(
            request.Institution,
            request.Degree,
            request.StartDate,
            request.EndDate);

        _unitOfWork.Educations.Update(existingEducation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return existingEducation.Id;
    }
}
