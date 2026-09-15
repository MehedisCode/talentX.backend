using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Users.Queries.GetAdminUserStats;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Users.Queries.GetAdminUserStats;

public sealed class GetAdminUserStatsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly GetAdminUserStatsQueryHandler _handler;

    public GetAdminUserStatsQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
        _handler = new GetAdminUserStatsQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ComputesTotalsFromRoleAndStatusCounts()
    {
        // Arrange
        var roleCounts = new Dictionary<UserRole, int>
        {
            [UserRole.Talent] = 10,
            [UserRole.Employer] = 4,
            [UserRole.Guest] = 0
        };
        var statusCounts = new Dictionary<AccountStatus, int>
        {
            [AccountStatus.Active] = 13,
            [AccountStatus.Deactivated] = 1
        };

        _userRepository
            .Setup(r => r.GetRoleAndStatusCountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((roleCounts, statusCounts));

        // Act
        var result = await _handler.Handle(new GetAdminUserStatsQuery(), CancellationToken.None);

        // Assert
        result.TalentsCount.Should().Be(10);
        result.EmployersCount.Should().Be(4);
        result.GuestsCount.Should().Be(0);
        result.TotalUsers.Should().Be(14);
        result.DeactivatedCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenNoUsersExist_ReturnsAllZeroCounts()
    {
        // Arrange
        _userRepository
            .Setup(r => r.GetRoleAndStatusCountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((new Dictionary<UserRole, int>(), new Dictionary<AccountStatus, int>()));

        // Act
        var result = await _handler.Handle(new GetAdminUserStatsQuery(), CancellationToken.None);

        // Assert
        result.TotalUsers.Should().Be(0);
        result.TalentsCount.Should().Be(0);
        result.EmployersCount.Should().Be(0);
        result.GuestsCount.Should().Be(0);
        result.DeactivatedCount.Should().Be(0);
    }
}
