using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Resumes.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Resumes.Commands.RenameResume;

public sealed class RenameResumeCommandHandler : IRequestHandler<RenameResumeCommand, SavedResumeDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RenameResumeCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<SavedResumeDto> Handle(
        RenameResumeCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!;

        var resume = await _unitOfWork.Resumes.GetByIdAsync(request.Id, cancellationToken);
        if (resume is null || resume.UserId != userId)
            throw new NotFoundException($"Resume '{request.Id}' was not found.");

        resume.Rename(request.DisplayName);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SavedResumeDto.FromEntity(resume);
    }
}
