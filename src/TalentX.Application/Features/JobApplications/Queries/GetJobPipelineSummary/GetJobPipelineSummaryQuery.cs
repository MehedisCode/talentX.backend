namespace TalentX.Application.Features.JobApplications.Queries.GetJobPipelineSummary;

using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;

public sealed record GetJobPipelineSummaryQuery(Guid JobId) : IRequest<JobPipelineSummaryDto>;
