using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Resumes.Queries.GetMyResumes;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Resumes;

public class GetMyResumesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IResumeRepository> _resumeRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetMyResumesQueryHandler _handler;

    public GetMyResumesQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Resumes).Returns(_resumeRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new GetMyResumesQueryHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_ReturnsMappedResumesAndCap()
    {
        var resume = new Resume(
            "user-1", "resumes/user-1/a.pdf", "a.pdf", "application/pdf", 1024,
            displayName: "Backend Resume", isDefault: true);
        _resumeRepository
            .Setup(r => r.GetAllByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Resume> { resume });

        var result = await _handler.Handle(new GetMyResumesQuery(), CancellationToken.None);

        result.MaxResumes.Should().Be(5);
        result.Resumes.Should().ContainSingle();
        result.Resumes[0].DisplayName.Should().Be("Backend Resume");
        result.Resumes[0].OriginalFileName.Should().Be("a.pdf");
        result.Resumes[0].IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserHasNoResumes_ReturnsEmptyList()
    {
        _resumeRepository
            .Setup(r => r.GetAllByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Resume>());

        var result = await _handler.Handle(new GetMyResumesQuery(), CancellationToken.None);

        result.Resumes.Should().BeEmpty();
        result.MaxResumes.Should().Be(5);
    }
}
