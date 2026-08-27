using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents.DTOs;

namespace TalentX.Application.Features.Talents.Queries.GetTalentApplicationPrefill;

public sealed class GetTalentApplicationPrefillQueryHandler
    : IRequestHandler<GetTalentApplicationPrefillQuery, TalentApplicationPrefillResponse>
{
    private static readonly TimeSpan DownloadUrlExpiry = TimeSpan.FromMinutes(15);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPresignedUrlService _presignedUrlService;
    private readonly IMapper _mapper;

    public GetTalentApplicationPrefillQueryHandler(
        IUnitOfWork unitOfWork,
        IPresignedUrlService presignedUrlService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _presignedUrlService = presignedUrlService;
        _mapper = mapper;
    }

    public async Task<TalentApplicationPrefillResponse> Handle(
        GetTalentApplicationPrefillQuery request,
        CancellationToken cancellationToken)
    {
        var talent = await _unitOfWork.Talents.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException($"Talent profile for user '{request.UserId}' was not found.");

        var workExperiences = await _unitOfWork.WorkExperiences.GetByUserIdAsync(request.UserId, cancellationToken);
        var resume = await _unitOfWork.Resumes.GetByUserIdAsync(request.UserId, cancellationToken);

        var resumeUrl = string.IsNullOrWhiteSpace(resume?.StorageKey)
            ? null
            : await _presignedUrlService.CreateDownloadUrlAsync(resume.StorageKey, DownloadUrlExpiry, cancellationToken);

        return new TalentApplicationPrefillResponse(
            talent.Name,
            talent.Email,
            talent.Phone,
            talent.Location,
            talent.ExpectedSalary,
            talent.TotalYearsExperience,
            resume?.StorageKey,
            resume?.OriginalFileName,
            resumeUrl,
            _mapper.Map<List<WorkExperienceItemDto>>(workExperiences)
        );
    }
}
