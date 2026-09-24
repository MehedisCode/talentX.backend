using System;
using System.Threading;
using System.Threading.Tasks;
using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IJobReportRepository
{
    Task<JobReport?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsForJobAndReporterAsync(Guid jobId, string reporterUserId, CancellationToken cancellationToken = default);
    
    Task AddAsync(JobReport jobReport, CancellationToken cancellationToken = default);
    
    Task<PaginatedList<JobReport>> GetReportsAsync(
        JobReportStatus? status,
        string? searchKeyword,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
