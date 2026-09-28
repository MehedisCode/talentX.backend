using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.InterviewPreparation;
using TalentX.Application.Features.InterviewPreparation.Queries.GetPreparationsByTalent;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using Xunit;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Queries.GetPreparationsByTalent;

public class GetPreparationsByTalentQueryHandlerTests
{
    private const string TalentUserId = "talent-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInterviewPreparationRepository> _preparationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetPreparationsByTalentQueryHandler _handler;

    public GetPreparationsByTalentQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.InterviewPreparations).Returns(_preparationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentUserId);

        _handler = new GetPreparationsByTalentQueryHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            Options.Create(new InterviewPreparationSettings()));
    }

    private static InterviewPreparationEntity CreatePreparation(string jobTitle)
    {
        return InterviewPreparationEntity.Create(
            new CreateInterviewPreparationPayload(
                InterviewInvitationId: Guid.NewGuid(),
                TalentUserId: TalentUserId,
                JobId: Guid.NewGuid(),
                JobTitle: jobTitle,
                CompanyName: "Acme",
                InterviewType: "OnCall",
                ScheduledAt: null,
                TechnicalQuestions: "[]",
                BehavioralQuestions: "[]",
                ExperienceBasedQuestions: "[]",
                RoleSpecificQuestions: "[]",
                KeyTopicsToPrepare: "[]",
                SuggestedRevisionAreas: "[]"));
    }

    [Fact]
    public async Task Handle_ReturnsAllPreparationsForTheSignedInTalent()
    {
        var first = CreatePreparation("Backend Engineer");
        var second = CreatePreparation("Platform Engineer");
        _preparationRepository
            .Setup(r => r.GetByTalentUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InterviewPreparationEntity> { first, second });

        var result = await _handler.Handle(
            new GetPreparationsByTalentQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].JobTitle.Should().Be("Backend Engineer");
        result[1].JobTitle.Should().Be("Platform Engineer");
        result.Should().OnlyContain(p => p.Status == "Generated" && p.GenerationCount == 1);
        _preparationRepository.Verify(
            r => r.GetByTalentUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTalentHasNoPreparations_ReturnsEmptyList()
    {
        _preparationRepository
            .Setup(r => r.GetByTalentUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InterviewPreparationEntity>());

        var result = await _handler.Handle(
            new GetPreparationsByTalentQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
