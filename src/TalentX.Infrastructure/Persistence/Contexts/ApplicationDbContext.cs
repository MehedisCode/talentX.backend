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
    public DbSet<JobApplicationStatusHistory> JobApplicationStatusHistories => Set<JobApplicationStatusHistory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<User> DomainUsers => Set<User>();
    public DbSet<Employer> Employers => Set<Employer>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Talent> Talents => Set<Talent>();
    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<SavedJob> SavedJobs => Set<SavedJob>();
    public DbSet<InterviewInvitation> InterviewInvitations => Set<InterviewInvitation>();
    public DbSet<InterviewTimeslot> InterviewTimeslots => Set<InterviewTimeslot>();
    public DbSet<EmailDispatchLog> EmailDispatchLogs => Set<EmailDispatchLog>();
    public DbSet<JobTalentMatch> JobTalentMatches => Set<JobTalentMatch>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<JobAlert> JobAlerts => Set<JobAlert>();
    public DbSet<TalentNotificationPreference> TalentNotificationPreferences => Set<TalentNotificationPreference>();
    public DbSet<JobAlertNotificationHistory> JobAlertNotificationHistories => Set<JobAlertNotificationHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<JobModerationHistory> JobModerationHistories => Set<JobModerationHistory>();
    public DbSet<InterviewPreparation> InterviewPreparations => Set<InterviewPreparation>();
    public DbSet<PreparationAnswer> PreparationAnswers => Set<PreparationAnswer>();
    public DbSet<InterviewPreparationEvaluation> InterviewPreparationEvaluations => Set<InterviewPreparationEvaluation>();
    public DbSet<InterviewPreparationConfig> InterviewPreparationConfigs => Set<InterviewPreparationConfig>();
    public DbSet<CompanyReview> CompanyReviews => Set<CompanyReview>();
    public DbSet<JobReport> JobReports => Set<JobReport>();
    public DbSet<AiConfiguration> AiConfigurations => Set<AiConfiguration>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
