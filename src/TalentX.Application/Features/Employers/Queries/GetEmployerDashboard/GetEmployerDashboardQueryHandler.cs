using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Employers.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Employers.Queries.GetEmployerDashboard;

public sealed class GetEmployerDashboardQueryHandler : IRequestHandler<GetEmployerDashboardQuery, EmployerDashboardDto>
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobApplicationRepository _jobApplicationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetEmployerDashboardQueryHandler(
        IJobRepository jobRepository,
        IJobApplicationRepository jobApplicationRepository,
        ICurrentUserService currentUserService)
    {
        _jobRepository = jobRepository;
        _jobApplicationRepository = jobApplicationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<EmployerDashboardDto> Handle(
        GetEmployerDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view employer dashboard.");

        var jobStats = await _jobRepository.GetEmployerStatsAsync(employerId, cancellationToken);
        var totalHires = await _jobApplicationRepository.CountHiredByEmployerIdAsync(employerId, cancellationToken);
        var recentJobs = await _jobRepository.GetRecentJobsByEmployerIdAsync(employerId, 5, cancellationToken);

        return new EmployerDashboardDto(
            jobStats.TotalPostedJobs,
            totalHires,
            jobStats.ActiveJobsCount,
            jobStats.DraftJobsCount,
            jobStats.ClosedJobsCount,
            jobStats.ExpiredJobsCount,
            recentJobs);
    }
}
