using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Resumes.Queries.GetResumeUrl;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Resumes;

public class GetResumeUrlQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IResumeRepository> _resumeRepository = new();
    private readonly Mock<IPresignedUrlService> _presignedUrlService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetResumeUrlQueryHandler _handler;

    public GetResumeUrlQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Resumes).Returns(_resumeRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new GetResumeUrlQueryHandler(
            _unitOfWork.Object,
            _presignedUrlService.Object,
            _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_ForOwnedResume_ReturnsPresignedUrl()
    {
        var resume = new Resume(
            "user-1", "resumes/user-1/a.pdf", "a.pdf", "application/pdf", 1024,
            displayName: "Backend Resume", isDefault: true);
        _resumeRepository
            .Setup(r => r.GetByIdAsync(resume.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);
        _presignedUrlService
            .Setup(s => s.CreateDownloadUrlAsync("resumes/user-1/a.pdf", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://signed.example.com/resumes/user-1/a.pdf");

        var result = await _handler.Handle(new GetResumeUrlQuery(resume.Id), CancellationToken.None);

        result.Url.Should().Be("https://signed.example.com/resumes/user-1/a.pdf");
        result.ContentType.Should().Be("application/pdf");
        result.DisplayName.Should().Be("Backend Resume");
    }

    [Fact]
    public async Task Handle_ForResumeOfAnotherUser_ThrowsNotFoundException_AndGeneratesNoUrl()
    {
        var resume = new Resume("other-user", "resumes/other-user/a.pdf", "a.pdf", "application/pdf", 1024);
        _resumeRepository
            .Setup(r => r.GetByIdAsync(resume.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);

        var act = () => _handler.Handle(new GetResumeUrlQuery(resume.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _presignedUrlService.Verify(
            s => s.CreateDownloadUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
