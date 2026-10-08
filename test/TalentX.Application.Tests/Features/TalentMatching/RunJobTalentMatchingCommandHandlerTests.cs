using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Helpers;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.TalentMatching.Commands.RunJobTalentMatching;
using TalentX.Application.Features.TalentMatching.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.TalentMatching;

public class RunJobTalentMatchingCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobRepository> _jobRepo = new();
    private readonly Mock<ITalentRepository> _talentRepo = new();
    private readonly Mock<IWorkExperienceRepository> _workExpRepo = new();
    private readonly Mock<IEducationRepository> _educationRepo = new();
    private readonly Mock<IJobTalentMatchRepository> _matchRepo = new();
    private readonly Mock<IInvitationRepository> _invitationRepo = new();
    private readonly Mock<ITalentMatchingService> _matchingService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ILogger<RunJobTalentMatchingCommandHandler>> _logger = new();
    private readonly RunJobTalentMatchingCommandHandler _handler;

    private TalentMatchingRequest? _capturedRequest;

    private const string CurrentEmployerId = "employer-user-1";
    private readonly Guid _jobId = Guid.NewGuid();

    public RunJobTalentMatchingCommandHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns(CurrentEmployerId);

        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepo.Object);
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepo.Object);
        _unitOfWork.SetupGet(u => u.WorkExperiences).Returns(_workExpRepo.Object);
        _unitOfWork.SetupGet(u => u.Educations).Returns(_educationRepo.Object);
        _unitOfWork.SetupGet(u => u.JobTalentMatches).Returns(_matchRepo.Object);
        _unitOfWork.SetupGet(u => u.Invitations).Returns(_invitationRepo.Object);

        _handler = new RunJobTalentMatchingCommandHandler(
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

    private static TalentProfileForMatching BuildProfile(Talent talent) => new()
    {
        TalentUserId = talent.UserId,
        Name = talent.Name,
        Tagline = talent.Tagline,
        TechnicalSkills = talent.TechnicalSkills ?? new(),
        SoftSkills = talent.SoftSkills ?? new(),
        ToolsAndPlatforms = talent.ToolsAndPlatforms ?? new(),
        TotalYearsExperience = talent.TotalYearsExperience,
        ExpectedSalary = talent.ExpectedSalary,
        Location = talent.Location,
        SkillPropositions = talent.SkillPropositions ?? new(),
        WorkExperiences = new(),
        Educations = new()
    };

    private void ArrangeHappyPath(Job job, params Talent[] talents)
    {
        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _talentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(talents.ToList());

        _workExpRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkExperience>());
        _educationRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Education>());
        _invitationRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invitation>());
    }

    private void SetupMatchingResult(params CandidateMatchResult[] matches)
    {
        _matchingService
            .Setup(s => s.MatchAsync(It.IsAny<TalentMatchingRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TalentMatchingRequest, CancellationToken>((request, _) => _capturedRequest = request)
            .ReturnsAsync(new TalentMatchingResult { Matches = matches.ToList() });
    }

    private static Talent CreateTalent(string userId, params string[] skills)
    {
        var talent = new Talent(userId, $"Name {userId}", $"{userId}@example.com");
        talent.UpdateSkills(skills.ToList(), new(), new());
        return talent;
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_JobNotFound_ThrowsNotFoundException()
    {
        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var act = () => _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenJobNotOwnedByEmployer_ThrowsForbiddenException()
    {
        var otherJob = Job.Create(
            "other-employer",
            "Engineer",
            "Other",
            "Desc",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Remote",
            WorkArrangement.FullRemote,
            new List<string> { "C#" },
            1,
            DateTime.UtcNow.AddDays(30));

        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherJob);

        var act = () => _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenJobIsDraft_ThrowsBadRequestException()
    {
        var draftJob = Job.Create(
            CurrentEmployerId,
            "Engineer",
            "Acme",
            "Desc",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Remote",
            WorkArrangement.FullRemote,
            new List<string> { "C#" },
            1,
            DateTime.UtcNow.AddDays(30)); // status is Draft

        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftJob);

        var act = () => _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*active or expired*");
    }

    [Fact]
    public async Task Handle_WhenJobIsExpired_RunsMatching()
    {
        var job = CreatePublishedJob();
        job.MarkAsExpired();
        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var talent = new Talent("talent-user-1", "Alice Smith", "alice@example.com");
        talent.UpdateSkills(new List<string> { "C#" }, new(), new());
        _talentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Talent> { talent });

        _workExpRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkExperience>());
        _educationRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Education>());

        _matchingService.Setup(s => s.MatchAsync(It.IsAny<TalentMatchingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TalentMatchingResult
            {
                Matches = new List<CandidateMatchResult>
                {
                    new() { TalentUserId = "talent-user-1", Score = 88.0m, Explanation = "Solid C# match." }
                }
            });

        _matchRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobTalentMatch>());
        _invitationRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invitation>());

        var response = await _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        response.MatchedCount.Should().Be(1);
        response.Candidates[0].TalentUserId.Should().Be("talent-user-1");

        _matchRepo.Verify(r => r.AddAsync(It.IsAny<JobTalentMatch>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSuccessful_CallsAiAndPersistsMatches()
    {
        var job = CreatePublishedJob();
        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var talent1 = new Talent("talent-user-1", "Alice Smith", "alice@example.com");
        talent1.UpdateSkills(new List<string> { "C#", ".NET Core", "Docker" }, new(), new());
        var talent2 = new Talent("talent-user-2", "Bob Jones", "bob@example.com");
        talent2.UpdateSkills(new List<string> { "Python", "Django" }, new(), new());

        _talentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Talent> { talent1, talent2 });

        _workExpRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkExperience>());
        _educationRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Education>());

        _matchingService.Setup(s => s.MatchAsync(It.IsAny<TalentMatchingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TalentMatchingResult
            {
                Matches = new List<CandidateMatchResult>
                {
                    new() { TalentUserId = "talent-user-1", Score = 92.5m, Explanation = "Strong C# background." },
                    new() { TalentUserId = "talent-user-2", Score = 45.0m, Explanation = "Python focus." }
                }
            });

        _matchRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobTalentMatch>());
        _invitationRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invitation>());

        var response = await _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        response.Should().NotBeNull();
        response.MatchedCount.Should().Be(2);
        response.Candidates[0].TalentUserId.Should().Be("talent-user-1");
        response.Candidates[0].Score.Should().Be(92.5m);
        response.Candidates[1].TalentUserId.Should().Be("talent-user-2");
        response.Candidates[1].Score.Should().Be(45.0m);

        _matchRepo.Verify(r => r.AddAsync(It.IsAny<JobTalentMatch>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAiFails_ThrowsBadGatewayExceptionAndDoesNotCorruptData()
    {
        var job = CreatePublishedJob();
        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var talent = new Talent("talent-1", "Alice", "alice@example.com");
        talent.UpdateSkills(new List<string> { "C#" }, new(), new());
        _talentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Talent> { talent });

        _workExpRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkExperience>());
        _educationRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Education>());
        _matchRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobTalentMatch>());

        _matchingService.Setup(s => s.MatchAsync(It.IsAny<TalentMatchingRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("AI service unavailable"));

        var act = () => _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        await act.Should().ThrowAsync<BadGatewayException>();
        _matchRepo.Verify(r => r.AddAsync(It.IsAny<JobTalentMatch>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InputHashMatchesStoredHash_DoesNotCallMatchingService()
    {
        var job = CreatePublishedJob();
        var talent = CreateTalent("talent-user-1", "C#", ".NET");
        ArrangeHappyPath(job, talent);

        var hash = MatchingInputHash.Compute(BuildJobProfile(job), BuildProfile(talent));
        var existingMatch = new JobTalentMatch(job.Id, talent.UserId, 90m, "Previous explanation", hash);
        _matchRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobTalentMatch> { existingMatch });

        var response = await _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        _matchingService.Verify(
            s => s.MatchAsync(It.IsAny<TalentMatchingRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        response.MatchedCount.Should().Be(1);
        response.ReusedCount.Should().Be(1);
        response.Candidates[0].Score.Should().Be(90m);
        existingMatch.Score.Should().Be(90m);
    }

    [Fact]
    public async Task Handle_NewTalentOnly_ScoresOnlyThatTalent()
    {
        var job = CreatePublishedJob();
        var reusedTalent = CreateTalent("talent-user-1", "C#", ".NET");
        var newTalent = CreateTalent("talent-user-2", "Python", "Django");
        ArrangeHappyPath(job, reusedTalent, newTalent);

        var hash = MatchingInputHash.Compute(BuildJobProfile(job), BuildProfile(reusedTalent));
        var existingMatch = new JobTalentMatch(job.Id, reusedTalent.UserId, 90m, "Previous", hash);
        _matchRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobTalentMatch> { existingMatch });

        SetupMatchingResult(
            new CandidateMatchResult { TalentUserId = newTalent.UserId, Score = 75m, Explanation = "Fresh score." });

        var response = await _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        _capturedRequest.Should().NotBeNull();
        _capturedRequest!.Candidates.Should().ContainSingle();
        _capturedRequest.Candidates[0].TalentUserId.Should().Be(newTalent.UserId);
        response.MatchedCount.Should().Be(2);
        response.ReusedCount.Should().Be(1);
        existingMatch.Score.Should().Be(90m);
    }

    [Fact]
    public async Task Handle_PartialReuse_ResponseContainsReusedAndNewCandidates()
    {
        var job = CreatePublishedJob();
        var reusedTalent = CreateTalent("talent-user-1", "C#", ".NET");
        var newTalent = CreateTalent("talent-user-2", "Python", "Django");
        ArrangeHappyPath(job, reusedTalent, newTalent);

        var hash = MatchingInputHash.Compute(BuildJobProfile(job), BuildProfile(reusedTalent));
        var existingMatch = new JobTalentMatch(job.Id, reusedTalent.UserId, 90m, "Previous", hash);
        _matchRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobTalentMatch> { existingMatch });

        SetupMatchingResult(
            new CandidateMatchResult { TalentUserId = newTalent.UserId, Score = 75m, Explanation = "Fresh score." });

        var response = await _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        response.MatchedCount.Should().Be(2);
        response.ReusedCount.Should().Be(1);
        response.Candidates.Should().HaveCount(2);
        response.Candidates.Select(c => c.TalentUserId)
            .Should().ContainInOrder(reusedTalent.UserId, newTalent.UserId);
        response.Candidates[0].Score.Should().Be(90m);
        response.Candidates[1].Score.Should().Be(75m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ScorePersisted_StoresInputHash()
    {
        var job = CreatePublishedJob();
        var talent = CreateTalent("talent-user-1", "C#", ".NET");
        ArrangeHappyPath(job, talent);
        _matchRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobTalentMatch>());

        var expectedHash = MatchingInputHash.Compute(BuildJobProfile(job), BuildProfile(talent));
        SetupMatchingResult(
            new CandidateMatchResult { TalentUserId = talent.UserId, Score = 92.5m, Explanation = "Strong match." });

        JobTalentMatch? added = null;
        _matchRepo
            .Setup(r => r.AddAsync(It.IsAny<JobTalentMatch>(), It.IsAny<CancellationToken>()))
            .Callback<JobTalentMatch, CancellationToken>((m, _) => added = m);

        var response = await _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        added.Should().NotBeNull();
        added!.InputHash.Should().Be(expectedHash);
        added.Score.Should().Be(92.5m);
        response.ReusedCount.Should().Be(0);
        response.MatchedCount.Should().Be(1);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ModelOmitsCandidate_KeepsPreviousMatchRow()
    {
        var job = CreatePublishedJob();
        var talent = CreateTalent("talent-user-1", "C#", ".NET");
        ArrangeHappyPath(job, talent);

        var staleHash = MatchingInputHash.Compute(
            BuildJobProfile(job), BuildProfile(talent), scoringVersion: "stale-version");
        var existingMatch = new JobTalentMatch(job.Id, talent.UserId, 88m, "Previous", staleHash);
        _matchRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobTalentMatch> { existingMatch });

        SetupMatchingResult();

        var response = await _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        existingMatch.Score.Should().Be(88m);
        existingMatch.InputHash.Should().Be(staleHash);
        response.MatchedCount.Should().Be(1);
        response.Candidates[0].Score.Should().Be(88m);
        _matchRepo.Verify(
            r => r.AddAsync(It.IsAny<JobTalentMatch>(), It.IsAny<CancellationToken>()), Times.Never);
        _matchRepo.Verify(r => r.Update(It.IsAny<JobTalentMatch>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ModelReturnsDuplicateTalentUserIds_DoesNotThrow()
    {
        var job = CreatePublishedJob();
        var talent = CreateTalent("talent-user-1", "C#", ".NET");
        ArrangeHappyPath(job, talent);
        _matchRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobTalentMatch>());

        SetupMatchingResult(
            new CandidateMatchResult { TalentUserId = talent.UserId, Score = 81m, Explanation = "First." },
            new CandidateMatchResult { TalentUserId = talent.UserId, Score = 99m, Explanation = "Duplicate." });

        JobTalentMatch? added = null;
        _matchRepo
            .Setup(r => r.AddAsync(It.IsAny<JobTalentMatch>(), It.IsAny<CancellationToken>()))
            .Callback<JobTalentMatch, CancellationToken>((m, _) => added = m);

        var response = await _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        added.Should().NotBeNull();
        added!.Score.Should().Be(81m);
        response.MatchedCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_TiedEligibleTalents_CutIsDeterministicByUserId()
    {
        var job = CreatePublishedJob();
        var talents = Enumerable.Range(0, 51)
            .Select(i => CreateTalent($"talent-{i:000}", "C#"))
            .ToArray();
        ArrangeHappyPath(job, talents);

        _configuration.Setup(c => c["GEMINI_MATCH_BATCH_SIZE"]).Returns("60");
        _matchRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobTalentMatch>());

        SetupMatchingResult(talents
            .Take(50)
            .Select(t => new CandidateMatchResult { TalentUserId = t.UserId, Score = 70m, Explanation = "ok" })
            .ToArray());

        var response = await _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        response.MatchedCount.Should().Be(50);
        _capturedRequest!.Candidates.Should().HaveCount(50);
        _capturedRequest.Candidates.Should().NotContain(c => c.TalentUserId == "talent-050");
        _capturedRequest.Candidates.Select(c => c.TalentUserId)
            .Should().BeEquivalentTo(talents.Take(50).Select(t => t.UserId));
    }
}
