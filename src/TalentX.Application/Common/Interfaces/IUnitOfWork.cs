using TalentX.Application.Common.Interfaces.Repositories;

namespace TalentX.Application.Common.Interfaces;

/// <summary>
/// Unit of work interface coordinating repository operations and transactions.
/// </summary>
public interface IUnitOfWork
{
    IJobRepository Jobs { get; }
    IJobApplicationRepository JobApplications { get; }
    IUserRepository Users { get; }
    IEmployerRepository Employers { get; }
    ITalentRepository Talents { get; }
    IResumeRepository Resumes { get; }
    IWorkExperienceRepository WorkExperiences { get; }
    IEducationRepository Educations { get; }

    /// <summary>
    /// Asynchronously persists all changes made in the context to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}