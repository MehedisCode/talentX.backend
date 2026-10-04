using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Resumes.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Resumes.Commands.SetDefaultResume;

public sealed class SetDefaultResumeCommandHandler
    : IRequestHandler<SetDefaultResumeCommand, SavedResumeDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SetDefaultResumeCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<SavedResumeDto> Handle(
        SetDefaultResumeCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!;

        var resumes = await _unitOfWork.Resumes.GetAllByUserIdAsync(userId, cancellationToken);
        var target = resumes.FirstOrDefault(resume => resume.Id == request.Id);
        if (target is null)
            throw new NotFoundException($"Resume '{request.Id}' was not found.");

        foreach (var resume in resumes)
        {
            if (resume.Id == target.Id)
                resume.SetDefault();
            else if (resume.IsDefault)
                resume.ClearDefault();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SavedResumeDto.FromEntity(target);
    }
}
