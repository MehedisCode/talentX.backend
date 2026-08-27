using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;

public sealed class ApplyExtractedProfileCommandHandler : IRequestHandler<ApplyExtractedProfileCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;

    public ApplyExtractedProfileCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(ApplyExtractedProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to update your profile.");

        TalentMediaStorageKeyGuard.EnsureOwnedByUser(request.ResumeStorageKey, userId, "resumes");

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"Talent profile for user '{userId}' was not found.");

        talent.UpdateSkills(request.TechnicalSkills, request.SoftSkills, request.ToolsAndPlatforms);
        _unitOfWork.Talents.Update(talent);

        await _unitOfWork.WorkExperiences.DeleteByUserIdAsync(userId, cancellationToken);
        foreach (var workExperience in request.WorkExperiences)
        {
            await _unitOfWork.WorkExperiences.AddAsync(
                new WorkExperience(
                    userId,
                    workExperience.CompanyName,
                    workExperience.JobTitle,
                    workExperience.StartDate,
                    workExperience.EndDate,
                    workExperience.IsCurrent),
                cancellationToken);
        }

        await _unitOfWork.Educations.DeleteByUserIdAsync(userId, cancellationToken);
        foreach (var education in request.Educations)
        {
            await _unitOfWork.Educations.AddAsync(
                new Education(
                    userId,
                    education.Institution,
                    education.Degree,
                    education.StartDate,
                    education.EndDate),
                cancellationToken);
        }

        var sanitizedResumeFileName = Path.GetFileName(request.ResumeOriginalFileName);
        var existingResume = await _unitOfWork.Resumes.GetByUserIdAsync(userId, cancellationToken);
        var previousResumeStorageKey = existingResume?.StorageKey;

        if (existingResume is null)
        {
            await _unitOfWork.Resumes.AddAsync(
                new Resume(
                    userId,
                    request.ResumeStorageKey,
                    sanitizedResumeFileName,
                    request.ResumeContentType,
                    request.ResumeSizeBytes),
                cancellationToken);
        }
        else
        {
            existingResume.UpdateFile(
                request.ResumeStorageKey,
                sanitizedResumeFileName,
                request.ResumeContentType,
                request.ResumeSizeBytes);
            _unitOfWork.Resumes.Update(existingResume);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousResumeStorageKey) &&
            previousResumeStorageKey != request.ResumeStorageKey)
        {
            await _fileStorageService.DeleteAsync(previousResumeStorageKey, cancellationToken);
        }
    }
}
