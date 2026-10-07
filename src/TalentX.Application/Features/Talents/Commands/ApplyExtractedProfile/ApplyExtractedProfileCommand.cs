using MediatR;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;

public sealed record ApplyExtractedProfileCommand(
    ApplyExtractedProfileData ProfileData,
    IUploadedFile ResumeFile
) : IRequest;
