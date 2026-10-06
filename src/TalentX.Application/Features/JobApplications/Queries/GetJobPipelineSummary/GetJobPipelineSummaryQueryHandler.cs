namespace TalentX.Application.Features.JobApplications.Queries.GetJobPipelineSummary;

using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;

public sealed class GetJobPipelineSummaryQueryHandler
    : IRequestHandler<GetJobPipelineSummaryQuery, JobPipelineSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetJobPipelineSummaryQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<JobPipelineSummaryDto> Handle(
        GetJobPipelineSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be authenticated to view the job pipeline summary.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.JobId}' was not found.");

        if (job.EmployerId != currentUserId)
        {
            throw new ForbiddenException("You do not have permission to view the pipeline summary for this job.");
        }

        var pipelineStatistics = await _unitOfWork.JobApplications.GetCandidatePipelineStatsByJobIdAsync(
            request.JobId,
            cancellationToken);

        var totalApplicants = pipelineStatistics.AppliedCount +
                              pipelineStatistics.ShortlistedCount +
                              pipelineStatistics.InvitedCount +
                              pipelineStatistics.InterviewCount +
                              pipelineStatistics.RejectedCount +
                              pipelineStatistics.HiredCount;

        return new JobPipelineSummaryDto(
            job.Id,
            job.Title,
            job.CompanyName,
            job.Status.ToString(),
            job.Vacancies,
            // The original posting date: the applicants span the job's whole life, not just since a reopen.
            job.FirstPublishedAt ?? job.CreatedAt,
            totalApplicants,
            pipelineStatistics);
    }
}
