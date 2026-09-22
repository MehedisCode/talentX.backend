using TalentX.Application.Common.Interfaces.Repositories;

namespace TalentX.Application.Common.Interfaces;

/// <summary>
/// Unit of work interface coordinating repository operations and transactions.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IJobRepository Jobs { get; }
    IJobApplicationRepository JobApplications { get; }
    IUserRepository Users { get; }
    IEmployerRepository Employers { get; }
    ICompanyRepository Companies { get; }
    ITalentRepository Talents { get; }
    IResumeRepository Resumes { get; }
    IWorkExperienceRepository WorkExperiences { get; }
    IEducationRepository Educations { get; }
    IInvitationRepository Invitations { get; }
    ISavedJobRepository SavedJobs { get; }
    IInterviewInvitationRepository InterviewInvitations { get; }
    IEmailDispatchLogRepository EmailDispatchLogs { get; }
    IJobTalentMatchRepository JobTalentMatches { get; }
    IAuditLogRepository AuditLogs { get; }
    IJobAlertRepository JobAlerts { get; }
    ITalentNotificationPreferenceRepository TalentNotificationPreferences { get; }
    IJobAlertNotificationHistoryRepository JobAlertNotificationHistories { get; }
    INotificationRepository Notifications { get; }
    IJobModerationHistoryRepository JobModerationHistories { get; }

    /// <summary>
    /// Asynchronously persists all changes made in the context to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}