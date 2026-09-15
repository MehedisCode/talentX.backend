using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.TalentMatching.Queries.GetMatchedCandidates;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.TalentMatching;

public class GetMatchedCandidatesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobRepository> _jobRepo = new();
    private readonly Mock<ITalentRepository> _talentRepo = new();
    private readonly Mock<IJobTalentMatchRepository> _matchRepo = new();
    private readonly Mock<IInvitationRepository> _invitationRepo = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetMatchedCandidatesQueryHandler _handler;

    private const string CurrentEmployerId = "employer-user-1";
    private readonly Guid _jobId = Guid.NewGuid();

    public GetMatchedCandidatesQueryHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns(CurrentEmployerId);

        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepo.Object);
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepo.Object);
        _unitOfWork.SetupGet(u => u.JobTalentMatches).Returns(_matchRepo.Object);
        _unitOfWork.SetupGet(u => u.Invitations).Returns(_invitationRepo.Object);

        _handler = new GetMatchedCandidatesQueryHandler(
            _unitOfWork.Object,
            _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenJobHasMatches_ReturnsRankedCandidatesWithInvitationFlag()
    {
        var job = Job.Create(
            CurrentEmployerId, "Dev", "Co", "Desc",
            EmploymentType.FullTime, ExperienceLevel.MidLevel, "Remote",
            WorkArrangement.FullRemote, new List<string> { "C#" }, 1,
            DateTime.UtcNow.AddDays(30));

        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var match1 = new JobTalentMatch(job.Id, "talent-1", 95.0m, "Top match");
        var match2 = new JobTalentMatch(job.Id, "talent-2", 75.0m, "Good match");
        _matchRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobTalentMatch> { match2, match1 });

        var talent1 = new Talent("talent-1", "Alice", "alice@example.com");
        var talent2 = new Talent("talent-2", "Bob", "bob@example.com");
        _talentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Talent> { talent1, talent2 });

        var jobExpireAt = DateTime.UtcNow.AddDays(30);
        var activeInvitation = new Invitation(job.Id, "talent-1", jobExpireAt);
        _invitationRepo.Setup(r => r.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invitation> { activeInvitation });

        var result = await _handler.Handle(new GetMatchedCandidatesQuery(_jobId), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].TalentUserId.Should().Be("talent-1");
        result[0].Score.Should().Be(95.0m);
        result[0].HasActiveInvitation.Should().BeTrue();

        result[1].TalentUserId.Should().Be("talent-2");
        result[1].Score.Should().Be(75.0m);
        result[1].HasActiveInvitation.Should().BeFalse();
    }
}
