using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Talents.Commands.ReplaceResume;

public sealed record ReplaceResumeCommand(IUploadedFile File) : IRequest<ResumeExtractionResult>;
