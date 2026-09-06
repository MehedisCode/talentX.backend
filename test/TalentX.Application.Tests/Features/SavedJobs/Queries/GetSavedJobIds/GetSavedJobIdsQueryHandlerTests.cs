using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.SavedJobs.Queries.GetSavedJobIds;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Tests.Features.SavedJobs.Queries.GetSavedJobIds;

public class GetSavedJobIdsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ISavedJobRepository> _savedJobRepository = new();
    private readonly GetSavedJobIdsQueryHandler _handler;

    public GetSavedJobIdsQueryHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("talent-1");
        _unitOfWork.Setup(u => u.SavedJobs).Returns(_savedJobRepository.Object);
        _handler = new GetSavedJobIdsQueryHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_ReturnsSavedJobIdsForTalent()
    {
        var jobIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        _savedJobRepository.Setup(r => r.GetSavedJobIdsAsync("talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobIds);

        var result = await _handler.Handle(new GetSavedJobIdsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(jobIds);
    }

    [Fact]
    public async Task Handle_WhenNoSavedJobs_ReturnsEmptyList()
    {
        _savedJobRepository.Setup(r => r.GetSavedJobIdsAsync("talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        var result = await _handler.Handle(new GetSavedJobIdsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new GetSavedJobIdsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_PassesCorrectTalentIdToRepository()
    {
        var jobIds = new List<Guid> { Guid.NewGuid() };
        _savedJobRepository.Setup(r => r.GetSavedJobIdsAsync("talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobIds);

        await _handler.Handle(new GetSavedJobIdsQuery(), CancellationToken.None);

        _savedJobRepository.Verify(r => r.GetSavedJobIdsAsync("talent-1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
