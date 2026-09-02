using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;

namespace TalentX.Application.Features.JobApplications.Queries.GetJobApplicants;

public sealed record GetJobApplicantsQuery(Guid JobId) : IRequest<IReadOnlyList<JobApplicantListItemDto>>;
