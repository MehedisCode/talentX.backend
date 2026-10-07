using FluentAssertions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Talents.Queries.GetTalentMatches;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Moq;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Queries.GetTalentMatches;

public class GetTalentMatchesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITalentRepository> _talentRepo = new();
    private readonly Mock<IJobRepository> _jobRepo = new();
    private readonly Mock<IJobApplicationRepository> _applicationRepo = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetTalentMatchesQueryHandler _handler;

    private const string CurrentTalentUserId = "talent-1";

    public GetTalentMatchesQueryHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns(CurrentTalentUserId);

        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepo.Object);
        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepo.Object);
        _unitOfWork.SetupGet(u => u.JobApplications).Returns(_applicationRepo.Object);

        _handler = new GetTalentMatchesQueryHandler(
            _unitOfWork.Object,
            _currentUserService.Object);
    }

    private static Job CreateJob(string title)
    {
        var job = Job.Create(
            "employer-1", title, "Co", "Desc",
            EmploymentType.FullTime, ExperienceLevel.MidLevel, "Remote",
            WorkArrangement.FullRemote, new List<string> { "C#" }, 1,
            DateTime.UtcNow.AddDays(30));
        job.MarkAsPublished();
        return job;
    }

    private void ArrangeTalentWithCSharpSkills()
    {
        var talent = new Talent(CurrentTalentUserId, "Ada", "ada@example.com");
        talent.UpdateSkills(new List<string> { "C#" }, new(), new());
        _talentRepo.Setup(r => r.GetByUserIdAsync(CurrentTalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);
    }

    [Fact]
    public async Task Handle_TiedMatchScores_ReturnsMatchesOrderedByJobId()
    {
        ArrangeTalentWithCSharpSkills();

        var first = CreateJob("Backend Engineer");
        var second = CreateJob("Platform Engineer");
        var jobs = new List<Job> { first, second };
        _jobRepo.Setup(r => r.GetActivePublishedJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobs.OrderByDescending(j => j.Id).ToList());

        _applicationRepo
            .Setup(r => r.ExistsByJobIdAndTalentUserIdAsync(It.IsAny<Guid>(), CurrentTalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new GetTalentMatchesQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Select(m => m.MatchScore).Should().OnlyContain(score => score == 100);
        result.Items.Select(m => m.JobId)
            .Should().ContainInOrder(new[] { first.Id, second.Id }.OrderBy(id => id));
    }

    [Fact]
    public async Task Handle_TalentWithoutSkills_ReturnsEmptyPage()
    {
        var talent = new Talent(CurrentTalentUserId, "Ada", "ada@example.com");
        _talentRepo.Setup(r => r.GetByUserIdAsync(CurrentTalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);

        var result = await _handler.Handle(new GetTalentMatchesQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}
