using MediatR;

namespace TalentX.Application.Features.Resumes.Commands.DeleteResume;

public sealed record DeleteResumeCommand(Guid Id) : IRequest;
