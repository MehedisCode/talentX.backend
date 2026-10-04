using MediatR;
using TalentX.Application.Features.Resumes.DTOs;

namespace TalentX.Application.Features.Resumes.Commands.RenameResume;

public sealed record RenameResumeCommand(Guid Id, string DisplayName) : IRequest<SavedResumeDto>;
