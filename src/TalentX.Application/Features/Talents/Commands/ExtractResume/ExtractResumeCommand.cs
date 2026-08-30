using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Talents.Commands.ExtractResume;

public sealed record ExtractResumeCommand(IUploadedFile File) : IRequest<ResumeExtractionResult>;
