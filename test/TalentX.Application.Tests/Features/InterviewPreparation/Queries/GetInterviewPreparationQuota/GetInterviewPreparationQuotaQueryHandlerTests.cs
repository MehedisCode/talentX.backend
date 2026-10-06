using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.InterviewPreparation;
using TalentX.Application.Features.InterviewPreparation.Queries.GetInterviewPreparationQuota;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using Xunit;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Queries.GetInterviewPreparationQuota;

public class GetInterviewPreparationQuotaQueryHandlerTests
{
    private const string TalentUserId = "talent-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInterviewPreparationRepository> _preparationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IInterviewPreparationConfigProvider> _configProvider = new();
    private readonly GetInterviewPreparationQuotaQueryHandler _handler;

    public GetInterviewPreparationQuotaQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.InterviewPreparations).Returns(_preparationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentUserId);
        _configProvider.Setup(c => c.GetConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(InterviewPreparationConfig.CreateDefault());

        _handler = new GetInterviewPreparationQuotaQueryHandler(
            _unitOfWork.Object, _currentUserService.Object, _configProvider.Object);
    }

    [Fact]
    public async Task Handle_ReturnsConfiguredLimitsAndTodaysUsage()
    {
        _preparationRepository
            .Setup(r => r.CountInitialGenerationsTodayByTalentAsync(
                TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _preparationRepository
            .Setup(r => r.CountAllGenerationsTodayByTalentAsync(
                TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var result = await _handler.Handle(
            new GetInterviewPreparationQuotaQuery(), CancellationToken.None);

        result.DailyGenerateLimit.Should().Be(5);
        result.DailyGenerationLimit.Should().Be(10);
        result.MaxRegenerationCount.Should().Be(3);
        result.AccessWindowAfterInterviewMinutes.Should().Be(30);
        result.GeneratesUsedToday.Should().Be(2);
        result.GenerationsUsedToday.Should().Be(4);
        result.RemainingGeneratesToday.Should().Be(3);
        result.RemainingGenerationsToday.Should().Be(6);
    }

    [Fact]
    public async Task Handle_WhenUsageExceedsLimit_RemainingNeverGoesNegative()
    {
        _preparationRepository
            .Setup(r => r.CountInitialGenerationsTodayByTalentAsync(
                TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);
        _preparationRepository
            .Setup(r => r.CountAllGenerationsTodayByTalentAsync(
                TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);

        var result = await _handler.Handle(
            new GetInterviewPreparationQuotaQuery(), CancellationToken.None);

        result.RemainingGeneratesToday.Should().Be(0);
        result.RemainingGenerationsToday.Should().Be(0);
    }
}
