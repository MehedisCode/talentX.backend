using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Resumes.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Resumes.Queries.GetMyResumes;

public sealed class GetMyResumesQueryHandler : IRequestHandler<GetMyResumesQuery, GetMyResumesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetMyResumesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetMyResumesResponse> Handle(
        GetMyResumesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!;

        var resumes = await _unitOfWork.Resumes.GetAllByUserIdAsync(userId, cancellationToken);

        return new GetMyResumesResponse(
            resumes.Select(SavedResumeDto.FromEntity).ToList(),
            Resume.MaxPerUser);
    }
}
