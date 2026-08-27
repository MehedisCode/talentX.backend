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
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}