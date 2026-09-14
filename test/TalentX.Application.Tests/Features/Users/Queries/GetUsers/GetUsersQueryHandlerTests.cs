using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Users.Queries.GetUsers;
using TalentX.Domain;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly GetUsersQueryHandler _handler;

    public GetUsersQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
        _handler = new GetUsersQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithDefaultPagination_CallsRepositoryWithNormalizedPaginationAndMapsDtos()
    {
        // Arrange
        var user = new Talent("talent-user-id", "Jordan Smith", "jordan@talentx.com");
        var items = new List<User> { user };
        const int totalCount = 1;
        var expectedParams = new UserQueryParams { Page = 1, PageSize = 20 };

        _userRepository
            .Setup(r => r.GetPagedAsync(expectedParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, totalCount));

        var query = new GetUsersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);

        var dto = result.Items[0];
        dto.Id.Should().Be(user.Id);
        dto.Name.Should().Be("Jordan Smith");
        dto.Email.Should().Be("jordan@talentx.com");
        dto.Role.Should().Be(UserRole.Talent);
        dto.Status.Should().Be(AccountStatus.Active);
        dto.IsVerified.Should().BeFalse();

        _userRepository.Verify(
            r => r.GetPagedAsync(expectedParams, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCustomParameters_PassesAllParametersToRepository()
    {
        // Arrange
        const string searchQuery = "acme";
        var expectedParams = new UserQueryParams
        {
            SearchQuery = searchQuery,
            Role = UserRole.Employer,
            Status = AccountStatus.Pending,
            Page = 2,
            PageSize = 10
        };

        _userRepository
            .Setup(r => r.GetPagedAsync(expectedParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User>(), 0));

        var query = new GetUsersQuery
        {
            SearchQuery = searchQuery,
            Role = UserRole.Employer,
            Status = AccountStatus.Pending,
            Page = 2,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();

        _userRepository.Verify(
            r => r.GetPagedAsync(expectedParams, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidPagination_NormalizesPageAndPageSize()
    {
        // Arrange
        var expectedParams = new UserQueryParams { Page = 1, PageSize = 20 };

        _userRepository
            .Setup(r => r.GetPagedAsync(expectedParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User>(), 0));

        var query = new GetUsersQuery { Page = -3, PageSize = -10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);

        _userRepository.Verify(
            r => r.GetPagedAsync(expectedParams, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
