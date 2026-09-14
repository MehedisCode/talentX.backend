using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.AuditLogs.Queries.GetAuditLogs;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.AuditLogs.Queries.GetAuditLogs;

public sealed class GetAuditLogsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly GetAuditLogsQueryHandler _handler;

    public GetAuditLogsQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.AuditLogs).Returns(_auditLogRepository.Object);
        _handler = new GetAuditLogsQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithDefaultPagination_CallsRepositoryWithNormalizedPaginationAndMapsDtos()
    {
        // Arrange
        var auditLog = new AuditLog("admin@talentx.com", "Insert · Job", isSystem: false);
        var items = new List<AuditLog> { auditLog };
        const int totalCount = 1;
        var expectedParams = new AuditLogQueryParams(null, null, null, 1, 20);

        _auditLogRepository
            .Setup(r => r.GetPagedAuditLogsAsync(
                expectedParams,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, totalCount));

        var query = new GetAuditLogsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);

        var dto = result.Items[0];
        dto.Id.Should().Be(auditLog.Id);
        dto.Actor.Should().Be("admin@talentx.com");
        dto.Action.Should().Be("Insert · Job");
        dto.IsSystem.Should().BeFalse();
        dto.Timestamp.Should().Be(auditLog.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

        _auditLogRepository.Verify(
            r => r.GetPagedAuditLogsAsync(expectedParams, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCustomParameters_PassesAllParametersToRepository()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);
        const string searchQuery = "system";
        var expectedParams = new AuditLogQueryParams(searchQuery, startDate, endDate, 2, 10);

        _auditLogRepository
            .Setup(r => r.GetPagedAuditLogsAsync(
                expectedParams,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AuditLog>(), 0));

        var query = new GetAuditLogsQuery(
            SearchQuery: searchQuery,
            StartDate: startDate,
            EndDate: endDate,
            Page: 2,
            PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();

        _auditLogRepository.Verify(
            r => r.GetPagedAuditLogsAsync(expectedParams, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidPagination_NormalizesPageAndPageSize()
    {
        // Arrange
        var expectedParams = new AuditLogQueryParams(null, null, null, 1, 20);

        _auditLogRepository
            .Setup(r => r.GetPagedAuditLogsAsync(
                expectedParams,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AuditLog>(), 0));

        var query = new GetAuditLogsQuery(Page: -3, PageSize: -10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);

        _auditLogRepository.Verify(
            r => r.GetPagedAuditLogsAsync(expectedParams, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
