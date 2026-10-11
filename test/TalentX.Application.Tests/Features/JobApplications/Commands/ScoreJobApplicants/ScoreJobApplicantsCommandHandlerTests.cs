using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Helpers;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobApplications.Commands.ScoreJobApplicants;
using TalentX.Application.Features.TalentMatching.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.JobApplications.Commands.ScoreJobApplicants;

public class ScoreJobApplicantsCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobRepository> _jobRepo = new();
    private readonly Mock<IJobApplicationRepository> _applicationRepo = new();
    private readonly Mock<ITalentRepository> _talentRepo = new();
    private readonly Mock<IWorkExperienceRepository> _workExpRepo = new();
    private readonly Mock<IEducationRepository> _educationRepo = new();
    private readonly Mock<ITalentMatchingService> _matchingService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ILogger<ScoreJobApplicantsCommandHandler>> _logger = new();
    private readonly ScoreJobApplicantsCommandHandler _handler;

    private readonly List<TalentMatchingRequest> _capturedRequests = new();

    private const string CurrentEmployerId = "employer-user-1";
    private readonly Guid _jobId = Guid.NewGuid();

    public ScoreJobApplicantsCommandHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns(CurrentEmployerId);

        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepo.Object);
        _unitOfWork.SetupGet(u => u.JobApplications).Returns(_applicationRepo.Object);
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepo.Object);
        _unitOfWork.SetupGet(u => u.WorkExperiences).Returns(_workExpRepo.Object);
        _unitOfWork.SetupGet(u => u.Educations).Returns(_educationRepo.Object);

        _handler = new ScoreJobApplicantsCommandHandler(
            _unitOfWork.Object,
            _matchingService.Object,
            _currentUserService.Object,
            _configuration.Object,
            _logger.Object);
    }

    private Job CreatePublishedJob()
    {
        var job = Job.Create(
            CurrentEmployerId,
            "Senior Backend Engineer",
            "Acme Tech",
            "Build robust distributed systems.",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            "Remote",
            WorkArrangement.FullRemote,
            new List<string> { "C#", ".NET", "PostgreSQL" },
            2,
            DateTime.UtcNow.AddDays(30));
        job.MarkAsPublished();
        return job;
    }

    private void ArrangeJob(Job job, params JobApplication[] applications)
    {
        _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _applicationRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(applications.ToList());
    }

    private static JobApplication CreateGuestApp(Guid jobId, string name = "Jane Doe")
    {
        var application = JobApplication.CreateGuestApplication(
            jobId, name, $"{name.Replace(" ", ".")}@example.com", "resume-key");
        // Unverified guests (PendingVerification) are filtered out before scoring; the
        // scoring pipeline only ever sees applications that passed email verification.
        application.UpdateStatus(ApplicationStatus.Applied);
        return application;
    }

    private static JobApplication CreatePendingGuestApp(Guid jobId, string name = "Jane Doe") =>
        JobApplication.CreateGuestApplication(jobId, name, $"{name.Replace(" ", ".")}@example.com", "resume-key");

    private static JobProfileForMatching BuildJobProfile(Job job) => new()
    {
        JobId = job.Id,
        Title = job.Title,
        CompanyName = job.CompanyName,
        Description = job.Description,
        RequiredSkills = job.RequiredSkills ?? new(),
        ExperienceLevel = job.ExperienceLevel.ToString(),
        Location = job.Location,
        WorkArrangement = job.WorkArrangement.ToString(),
        City = job.City,
        Country = job.Country
    };

    private static TalentProfileForMatching MinimalProfile(JobApplication application) => new()
    {
        TalentUserId = application.TalentUserId ?? $"guest-{application.Id}",
        Name = application.ApplicantName,
        TotalYearsExperience = application.TotalYearsExperience,
        ExpectedSalary = application.ExpectedSalary,
        Location = application.Location
    };

    private void SetupMatchingEcho()
    {
        _matchingService
            .Setup(s => s.MatchAsync(It.IsAny<TalentMatchingRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TalentMatchingRequest, CancellationToken>((request, _) => _capturedRequests.Add(request))
            .Returns((TalentMatchingRequest request, CancellationToken _) =>
                Task.FromResult(new TalentMatchingResult
                {
                    Matches = request.Candidates
                        .Select(c => new CandidateMatchResult
                        {
                            TalentUserId = c.TalentUserId,
                            Score = 60m,
                            Explanation = "Echo score."
                        })
                        .ToList()
                }));
    }

    [Fact]
    public async Task Handle_InputHashMatchesStoredHash_DoesNotCallMatchingService()
    {
        var job = CreatePublishedJob();
        var app = CreateGuestApp(job.Id);
        ArrangeJob(job, app);

        var hash = MatchingInputHash.Compute(BuildJobProfile(job), MinimalProfile(app));
        app.SetAiMatchScore(77m, hash);

        var response = await _handler.Handle(new ScoreJobApplicantsCommand(job.Id), CancellationToken.None);

        _matchingService.Verify(
            s => s.MatchAsync(It.IsAny<TalentMatchingRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        response.ScoredCount.Should().Be(0);
        response.ReusedCount.Should().Be(1);
        app.AiMatchScore.Should().Be(77m);
    }

    [Fact]
    public async Task Handle_AllApplicantsReused_ReturnsWithoutSaving()
    {
        var job = CreatePublishedJob();
        var first = CreateGuestApp(job.Id, "Jane Doe");
        var second = CreateGuestApp(job.Id, "John Smith");
        ArrangeJob(job, first, second);

        first.SetAiMatchScore(70m, MatchingInputHash.Compute(BuildJobProfile(job), MinimalProfile(first)));
        second.SetAiMatchScore(80m, MatchingInputHash.Compute(BuildJobProfile(job), MinimalProfile(second)));

        var response = await _handler.Handle(new ScoreJobApplicantsCommand(job.Id), CancellationToken.None);

        response.ScoredCount.Should().Be(0);
        response.ReusedCount.Should().Be(2);
        _matchingService.Verify(
            s => s.MatchAsync(It.IsAny<TalentMatchingRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NewApplicantOnly_ScoresOnlyThatApplicant()
    {
        var job = CreatePublishedJob();
        var reusedApp = CreateGuestApp(job.Id, "Jane Doe");
        var newApp = CreateGuestApp(job.Id, "John Smith");
        ArrangeJob(job, reusedApp, newApp);

        reusedApp.SetAiMatchScore(
            91m, MatchingInputHash.Compute(BuildJobProfile(job), MinimalProfile(reusedApp)));

        SetupMatchingEcho();

        var response = await _handler.Handle(new ScoreJobApplicantsCommand(job.Id), CancellationToken.None);

        _capturedRequests.Should().ContainSingle();
        _capturedRequests[0].Candidates.Should().ContainSingle();
        _capturedRequests[0].Candidates[0].TalentUserId.Should().Be($"guest-{newApp.Id}");
        response.ScoredCount.Should().Be(1);
        response.ReusedCount.Should().Be(1);
        reusedApp.AiMatchScore.Should().Be(91m);
        newApp.AiMatchScore.Should().Be(60m);
    }

    [Fact]
    public async Task Handle_ScorePersisted_StoresInputHash()
    {
        var job = CreatePublishedJob();
        var app = CreateGuestApp(job.Id);
        ArrangeJob(job, app);

        var expectedHash = MatchingInputHash.Compute(BuildJobProfile(job), MinimalProfile(app));
        SetupMatchingEcho();

        var response = await _handler.Handle(new ScoreJobApplicantsCommand(job.Id), CancellationToken.None);

        app.AiMatchScore.Should().Be(60m);
        app.AiScoreInputHash.Should().Be(expectedHash);
        response.ScoredCount.Should().Be(1);
        _applicationRepo.Verify(r => r.Update(app), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ModelReturnsDuplicateTalentUserIds_DoesNotThrow()
    {
        var job = CreatePublishedJob();
        var app = CreateGuestApp(job.Id);
        ArrangeJob(job, app);

        var key = $"guest-{app.Id}";
        _matchingService
            .Setup(s => s.MatchAsync(It.IsAny<TalentMatchingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TalentMatchingResult
            {
                Matches = new List<CandidateMatchResult>
                {
                    new() { TalentUserId = key, Score = 81m, Explanation = "First." },
                    new() { TalentUserId = key, Score = 99m, Explanation = "Duplicate." }
                }
            });

        var response = await _handler.Handle(new ScoreJobApplicantsCommand(job.Id), CancellationToken.None);

        response.ScoredCount.Should().Be(1);
        app.AiMatchScore.Should().Be(81m);
    }

    [Fact]
    public async Task Handle_ModelOmitsCandidate_KeepsPreviousScoreAndHash()
    {
        var job = CreatePublishedJob();
        var app = CreateGuestApp(job.Id);
        ArrangeJob(job, app);

        var staleHash = MatchingInputHash.Compute(
            BuildJobProfile(job), MinimalProfile(app), scoringVersion: "stale-version");
        app.SetAiMatchScore(50m, staleHash);

        _matchingService
            .Setup(s => s.MatchAsync(It.IsAny<TalentMatchingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TalentMatchingResult { Matches = new List<CandidateMatchResult>() });

        var response = await _handler.Handle(new ScoreJobApplicantsCommand(job.Id), CancellationToken.None);

        app.AiMatchScore.Should().Be(50m);
        app.AiScoreInputHash.Should().Be(staleHash);
        response.ScoredCount.Should().Be(0);
        response.ReusedCount.Should().Be(0);
        _applicationRepo.Verify(r => r.Update(It.IsAny<JobApplication>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExceedsBatchSize_PartitionsAndPersistsPerBatch()
    {
        var job = CreatePublishedJob();
        var apps = Enumerable.Range(0, 5).Select(i => CreateGuestApp(job.Id, $"Applicant {i}")).ToArray();
        ArrangeJob(job, apps);

        _configuration.Setup(c => c["GEMINI_MATCH_BATCH_SIZE"]).Returns("2");
        SetupMatchingEcho();

        var response = await _handler.Handle(new ScoreJobApplicantsCommand(job.Id), CancellationToken.None);

        _capturedRequests.Should().HaveCount(3);
        _capturedRequests.Select(r => r.Candidates.Count).Should().Equal(2, 2, 1);
        _capturedRequests.SelectMany(r => r.Candidates).Should().HaveCount(5);
        response.ScoredCount.Should().Be(5);
        response.ReusedCount.Should().Be(0);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_MatchingServiceThrows_ThrowsBadGatewayExceptionAfterPersistingCompletedBatches()
    {
        var job = CreatePublishedJob();
        var apps = Enumerable.Range(0, 4).Select(i => CreateGuestApp(job.Id, $"Applicant {i}")).ToArray();
        ArrangeJob(job, apps);

        _configuration.Setup(c => c["GEMINI_MATCH_BATCH_SIZE"]).Returns("2");

        _matchingService
            .SetupSequence(s => s.MatchAsync(It.IsAny<TalentMatchingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TalentMatchingResult
            {
                Matches = apps.Take(2)
                    .Select(a => new CandidateMatchResult
                    {
                        TalentUserId = $"guest-{a.Id}",
                        Score = 61m,
                        Explanation = "Batch one."
                    })
                    .ToList()
            })
            .ThrowsAsync(new InvalidOperationException("AI service unavailable"));

        var act = () => _handler.Handle(new ScoreJobApplicantsCommand(job.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BadGatewayException>();

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        apps[0].AiMatchScore.Should().Be(61m);
        apps[1].AiMatchScore.Should().Be(61m);
        apps[2].AiMatchScore.Should().BeNull();
        apps[3].AiMatchScore.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PendingVerificationApplications_AreExcludedFromScoring()
    {
        var job = CreatePublishedJob();
        var verified = CreateGuestApp(job.Id, "Jane Doe");
        var pendingGuest = CreatePendingGuestApp(job.Id, "John Smith");
        ArrangeJob(job, verified, pendingGuest);

        SetupMatchingEcho();

        var response = await _handler.Handle(new ScoreJobApplicantsCommand(job.Id), CancellationToken.None);

        pendingGuest.Status.Should().Be(ApplicationStatus.PendingVerification);
        pendingGuest.AiMatchScore.Should().BeNull();
        verified.AiMatchScore.Should().Be(60m);
        _capturedRequests.Should().ContainSingle();
        _capturedRequests[0].Candidates.Should().ContainSingle();
        _capturedRequests[0].Candidates[0].TalentUserId.Should().Be($"guest-{verified.Id}");
        response.ScoredCount.Should().Be(1);
        response.ReusedCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_OnlyPendingVerificationApplications_SkipsMatchingServiceEntirely()
    {
        var job = CreatePublishedJob();
        ArrangeJob(job, CreatePendingGuestApp(job.Id, "John Smith"));

        var response = await _handler.Handle(new ScoreJobApplicantsCommand(job.Id), CancellationToken.None);

        _matchingService.Verify(
            s => s.MatchAsync(It.IsAny<TalentMatchingRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        response.ScoredCount.Should().Be(0);
        response.ReusedCount.Should().Be(0);
    }
}
