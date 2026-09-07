using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Identity.Models;

namespace TalentX.Infrastructure.Persistence.Contexts;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<User> DomainUsers => Set<User>();
    public DbSet<Employer> Employers => Set<Employer>();
    public DbSet<Talent> Talents => Set<Talent>();
    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<InterviewInvitation> InterviewInvitations => Set<InterviewInvitation>();
    public DbSet<InterviewTimeslot> InterviewTimeslots => Set<InterviewTimeslot>();
    public DbSet<EmailDispatchLog> EmailDispatchLogs => Set<EmailDispatchLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
