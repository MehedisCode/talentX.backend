using Microsoft.EntityFrameworkCore;
using Npgsql;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    public IJobRepository Jobs { get; }
    public IJobApplicationRepository JobApplications { get; }
    public IUserRepository Users { get; }
    public IEmployerRepository Employers { get; }
    public ITalentRepository Talents { get; }
    public IResumeRepository Resumes { get; }
    public IWorkExperienceRepository WorkExperiences { get; }
    public IEducationRepository Educations { get; }
    public IInvitationRepository Invitations { get; }
    public IJobTalentMatchRepository JobTalentMatches { get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Jobs = new JobRepository(_context);
        JobApplications = new JobApplicationRepository(_context);
        Users = new UserRepository(_context);
        Employers = new EmployerRepository(_context);
        Talents = new TalentRepository(_context);
        Resumes = new ResumeRepository(_context);
        WorkExperiences = new WorkExperienceRepository(_context);
        Educations = new EducationRepository(_context);
        Invitations = new InvitationRepository(_context);
        JobTalentMatches = new JobTalentMatchRepository(_context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // A concurrent insert won the race to a unique index (e.g. two simultaneous
            // job applications from the same Talent). Surface it as a 409 instead of a 500;
            // handlers' own pre-checks provide the specific, friendly message in the normal case.
            throw new ConflictException(
                "This action conflicts with an existing record. Please refresh and try again.");
        }
    }
}