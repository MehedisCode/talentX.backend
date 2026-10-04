using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Resumes.DTOs;
using TalentX.Application.Features.Talents.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Queries.GetTalentApplicationPrefill;

public sealed class GetTalentApplicationPrefillQueryHandler
    : IRequestHandler<GetTalentApplicationPrefillQuery, TalentApplicationPrefillResponse>
{
    private static readonly TimeSpan DownloadUrlExpiry = TimeSpan.FromMinutes(15);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPresignedUrlService _presignedUrlService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetTalentApplicationPrefillQueryHandler(
        IUnitOfWork unitOfWork,
        IPresignedUrlService presignedUrlService,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _presignedUrlService = presignedUrlService;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<TalentApplicationPrefillResponse> Handle(
        GetTalentApplicationPrefillQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view your application pre-fill.");

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"Talent profile for user '{userId}' was not found.");

        var workExperiences = await _unitOfWork.WorkExperiences.GetByUserIdAsync(userId, cancellationToken);
        var resumes = await _unitOfWork.Resumes.GetAllByUserIdAsync(userId, cancellationToken);
        var defaultResume = resumes.FirstOrDefault(resume => resume.IsDefault) ?? resumes.FirstOrDefault();

        var resumeUrl = string.IsNullOrWhiteSpace(defaultResume?.StorageKey)
            ? null
            : await _presignedUrlService.CreateDownloadUrlAsync(
                defaultResume.StorageKey, DownloadUrlExpiry, cancellationToken);

        return new TalentApplicationPrefillResponse(
            talent.Name,
            talent.Email,
            talent.Phone,
            talent.Location,
            talent.ExpectedSalary,
            talent.TotalYearsExperience,
            defaultResume?.StorageKey,
            defaultResume?.OriginalFileName,
            resumeUrl,
            resumes.Select(SavedResumeDto.FromEntity).ToList(),
            _mapper.Map<List<WorkExperienceItemDto>>(workExperiences)
        );
    }
}
