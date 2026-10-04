using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Resumes.DTOs;

namespace TalentX.Application.Features.Resumes.Commands.SaveResume;

public sealed record SaveResumeCommand(
    IUploadedFile ResumeFile,
    string? DisplayName,
    bool UpdateProfile
) : IRequest<SavedResumeDto>;
