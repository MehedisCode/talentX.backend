using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Commands.DeleteEducation;

public sealed class DeleteEducationCommandHandler : IRequestHandler<DeleteEducationCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteEducationCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteEducationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to manage your education.");

        var education = await _unitOfWork.Educations.GetByIdAsync(request.Id, cancellationToken);
        if (education is null || education.UserId != userId)
            throw new NotFoundException($"Education '{request.Id}' was not found.");

        _unitOfWork.Educations.Delete(education);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
