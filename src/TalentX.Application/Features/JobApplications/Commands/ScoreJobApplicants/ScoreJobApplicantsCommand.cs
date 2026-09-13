using MediatR;

namespace TalentX.Application.Features.JobApplications.Commands.ScoreJobApplicants;

public sealed record ScoreJobApplicantsCommand(Guid JobId) : IRequest<ScoreJobApplicantsResponse>;
