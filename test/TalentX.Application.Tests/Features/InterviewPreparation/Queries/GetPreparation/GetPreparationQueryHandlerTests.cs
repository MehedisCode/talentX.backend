using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.InterviewPreparation.Queries.GetPreparation;
using TalentX.Application.Identity.Services;
using Xunit;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Queries.GetPreparation;

public class GetPreparationQueryHandlerTests
{
    private const string TalentUserId = "talent-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInterviewPreparationRepository> _preparationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetPreparationQueryHandler _handler;

    public GetPreparationQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.InterviewPreparations).Returns(_preparationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentUserId);

        _handler = new GetPreparationQueryHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    private static InterviewPreparationEntity CreatePreparation(string? talentUserId = null)
    {
        return InterviewPreparationEntity.Create(
            interviewInvitationId: Guid.NewGuid(),
            talentUserId: talentUserId ?? TalentUserId,
            jobId: Guid.NewGuid(),
            jobTitle: "Backend Engineer",
            companyName: "Acme",
            interviewType: "OnCall",
            scheduledAt: DateTime.UtcNow.AddHours(2),
            technicalQuestions: """[{"question":"What is a closure?","category":"Technical","tip":"Scope"}]""",
            behavioralQuestions: "[]",
            experienceBasedQuestions: "[]",
            roleSpecificQuestions: "[]",
            keyTopicsToPrepare: """["Idempotency"]""",
            suggestedRevisionAreas: """["SQL tuning"]""");
    }

    [Fact]
    public async Task Handle_WhenPreparationExists_ReturnsMappedDto()
    {
        var preparation = CreatePreparation();
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(preparation.InterviewInvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preparation);

        var result = await _handler.Handle(
            new GetPreparationQuery(preparation.InterviewInvitationId), CancellationToken.None);

        result.Id.Should().Be(preparation.Id);
        result.InterviewInvitationId.Should().Be(preparation.InterviewInvitationId);
        result.JobTitle.Should().Be("Backend Engineer");
        result.Status.Should().Be("Generated");
        result.GenerationCount.Should().Be(1);
        result.IsAccessible.Should().BeTrue();
        result.TechnicalQuestions.Should().ContainSingle()
            .Which.Question.Should().Be("What is a closure?");
        result.KeyTopicsToPrepare.Should().Equal("Idempotency");
        result.SuggestedRevisionAreas.Should().Equal("SQL tuning");
        result.AdditionalQuestions.Should().BeEmpty();
        result.Disclaimer.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenPreparationDoesNotExist_ThrowsNotFoundException()
    {
        var invitationId = Guid.NewGuid();
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InterviewPreparationEntity?)null);

        var act = () => _handler.Handle(
            new GetPreparationQuery(invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Interview preparation not found.");
    }

    [Fact]
    public async Task Handle_WhenPreparationBelongsToAnotherTalent_ThrowsForbiddenException()
    {
        var preparation = CreatePreparation(talentUserId: "talent-2");
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(preparation.InterviewInvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preparation);

        var act = () => _handler.Handle(
            new GetPreparationQuery(preparation.InterviewInvitationId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this interview preparation.");
    }
}
