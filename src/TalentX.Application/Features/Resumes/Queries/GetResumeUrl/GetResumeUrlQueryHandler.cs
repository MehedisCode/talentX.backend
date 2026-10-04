using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Resumes.Queries.GetResumeUrl;

public sealed class GetResumeUrlQueryHandler : IRequestHandler<GetResumeUrlQuery, GetResumeUrlResponse>
{
    private static readonly TimeSpan DownloadUrlExpiry = TimeSpan.FromMinutes(15);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPresignedUrlService _presignedUrlService;
    private readonly ICurrentUserService _currentUserService;

    public GetResumeUrlQueryHandler(
        IUnitOfWork unitOfWork,
        IPresignedUrlService presignedUrlService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _presignedUrlService = presignedUrlService;
        _currentUserService = currentUserService;
    }

    public async Task<GetResumeUrlResponse> Handle(
        GetResumeUrlQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!;

        var resume = await _unitOfWork.Resumes.GetByIdAsync(request.Id, cancellationToken);
        if (resume is null || resume.UserId != userId)
            throw new NotFoundException($"Resume '{request.Id}' was not found.");

        var url = await _presignedUrlService.CreateDownloadUrlAsync(
            resume.StorageKey,
            DownloadUrlExpiry,
            cancellationToken);

        return new GetResumeUrlResponse(
            resume.Id,
            resume.OriginalFileName,
            resume.DisplayName,
            resume.ContentType,
            url);
    }
}
