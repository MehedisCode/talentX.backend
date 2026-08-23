using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Resumes.Commands.UploadResume;

public sealed record UploadResumeCommand(IUploadedFile File) : IRequest<ResumeExtractionResult>;
