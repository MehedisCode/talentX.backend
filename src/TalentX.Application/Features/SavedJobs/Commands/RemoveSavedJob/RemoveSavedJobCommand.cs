using MediatR;

namespace TalentX.Application.Features.SavedJobs.Commands.RemoveSavedJob;

public record RemoveSavedJobCommand(Guid JobId) : IRequest;
