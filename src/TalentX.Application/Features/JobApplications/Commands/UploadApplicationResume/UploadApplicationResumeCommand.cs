using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;

namespace TalentX.Application.Features.JobApplications.Commands.UploadApplicationResume;

public record UploadApplicationResumeCommand(IUploadedFile File)
    : IRequest<UploadApplicationResumeResponse>;
