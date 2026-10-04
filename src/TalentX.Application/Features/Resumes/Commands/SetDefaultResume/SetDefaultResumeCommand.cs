using MediatR;
using TalentX.Application.Features.Resumes.DTOs;

namespace TalentX.Application.Features.Resumes.Commands.SetDefaultResume;

public sealed record SetDefaultResumeCommand(Guid Id) : IRequest<SavedResumeDto>;
