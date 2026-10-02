using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
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
    private readonly RunJobTalentMatchingCommandHandler _handler;

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
            _currentUserService.Object);
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
    public async Task Handle_WhenAiFails_PropagatesExceptionAndDoesNotCorruptData()
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

        _matchingService.Setup(s => s.MatchAsync(It.IsAny<TalentMatchingRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("AI service unavailable"));

        var act = () => _handler.Handle(new RunJobTalentMatchingCommand(_jobId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _matchRepo.Verify(r => r.AddAsync(It.IsAny<JobTalentMatch>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
