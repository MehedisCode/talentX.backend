using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Companies.Queries.GetPublicCompanyProfile;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Companies;

public sealed class GetPublicCompanyProfileQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICompanyRepository> _companyRepositoryMock = new();
    private readonly Mock<IJobRepository> _jobRepositoryMock = new();
    private readonly Mock<IPresignedUrlService> _presignedUrlServiceMock = new();

    public GetPublicCompanyProfileQueryHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Companies).Returns(_companyRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Jobs).Returns(_jobRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPublicCompanyProfile_WhenCompanyExists()
    {
        var companyId = Guid.NewGuid();
        var company = new Company("employer-user-123", "Acme Global");
        company.UpdateProfile(
            "Acme Global",
            "Global provider",
            "Cloud",
            "501-1000",
            "https://acme.org",
            "Austin, TX",
            null, null, null
        );

        _companyRepositoryMock.Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        var activeJob = Job.Create(
            "employer-user-123",
            "Senior Backend Engineer",
            "Acme Global",
            "Exciting role",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            "Remote",
            WorkArrangement.FullRemote,
            new List<string> { "C#", ".NET" },
            2,
            DateTime.UtcNow.AddDays(30)
        );
        activeJob.MarkAsPublished();

        _jobRepositoryMock.Setup(r => r.GetActivePublishedJobsByEmployerIdAsync("employer-user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { activeJob });

        var handler = new GetPublicCompanyProfileQueryHandler(
            _unitOfWorkMock.Object,
            _presignedUrlServiceMock.Object
        );

        var result = await handler.Handle(new GetPublicCompanyProfileQuery(companyId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Acme Global", result.Name);
        Assert.Equal("Global provider", result.Description);
        Assert.Equal(1, result.ActiveJobsCount);
        Assert.Single(result.PublishedJobs);
        Assert.Equal("Senior Backend Engineer", result.PublishedJobs[0].Title);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenCompanyDoesNotExist()
    {
        var missingId = Guid.NewGuid();
        _companyRepositoryMock.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);

        var handler = new GetPublicCompanyProfileQueryHandler(
            _unitOfWorkMock.Object,
            _presignedUrlServiceMock.Object
        );

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetPublicCompanyProfileQuery(missingId), CancellationToken.None));
    }
}
