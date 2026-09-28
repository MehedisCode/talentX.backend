using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Companies.DTOs;
using TalentX.Application.Features.Companies.Queries.GetCompanies;
using TalentX.Domain;
using Xunit;

namespace TalentX.Application.Tests.Features.Companies;

public sealed class GetCompaniesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICompanyRepository> _companyRepositoryMock = new();
    private readonly Mock<IPresignedUrlService> _presignedUrlServiceMock = new();

    public GetCompaniesQueryHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Companies).Returns(_companyRepositoryMock.Object);
    }

    private static Company BuildCompany(string employerId, string name)
    {
        var company = new Company(employerId, name);
        company.UpdateProfile(name, "Desc", "Software", "11-50", "https://acme.example", "Dhaka", null, null, null);
        return company;
    }

    private static CompanySearchItem ToSearchItem(Company company, int openings, int vacancies, string? logoStorageKey = null) => new(
        company.Id,
        company.EmployerId,
        company.Name,
        logoStorageKey,
        "Desc",
        "Software",
        "11-50",
        "https://acme.example",
        "Dhaka",
        null,
        null,
        null,
        DateTime.UtcNow,
        openings,
        vacancies);

    [Fact]
    public async Task Handle_ReturnsMappedItems_WithOpeningsAndVacanciesCounts()
    {
        var company = BuildCompany("employer-1", "Acme");
        var paged = new CompanySearchResult(
            new List<CompanySearchItem> { ToSearchItem(company, openings: 3, vacancies: 7) },
            Page: 1,
            PageSize: 10,
            TotalCount: 1,
            TotalPages: 1,
            TotalOpenings: 3,
            TotalVacancies: 7);

        _companyRepositoryMock
            .Setup(r => r.GetAllAsync("acme", null, null, null, "name", "asc", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var handler = new GetCompaniesQueryHandler(_unitOfWorkMock.Object, _presignedUrlServiceMock.Object);

        var result = await handler.Handle(
            new GetCompaniesQuery(Keyword: "acme", Page: 1, PageSize: 10),
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(3, result.TotalOpenings);
        Assert.Equal(7, result.TotalVacancies);
        var item = Assert.Single(result.Items);
        Assert.Equal(company.Id, item.Id);
        Assert.Equal("Acme", item.Name);
        Assert.Equal("Software", item.Industry);
        Assert.Equal(3, item.OpeningsCount);
        Assert.Equal(7, item.VacanciesCount);
    }

    [Fact]
    public async Task Handle_NormalizesInvalidPaging_ToDefaults()
    {
        var paged = new CompanySearchResult(
            new List<CompanySearchItem>(),
            Page: 1,
            PageSize: 10,
            TotalCount: 0,
            TotalPages: 0,
            TotalOpenings: 0,
            TotalVacancies: 0);

        _companyRepositoryMock
            .Setup(r => r.GetAllAsync(null, null, null, null, "name", "asc", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var handler = new GetCompaniesQueryHandler(_unitOfWorkMock.Object, _presignedUrlServiceMock.Object);

        var result = await handler.Handle(
            new GetCompaniesQuery(Page: 0, PageSize: 500),
            CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        _companyRepositoryMock.Verify(
            r => r.GetAllAsync(null, null, null, null, "name", "asc", 1, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ResolvesLogoUrl_WhenLogoStorageKeyPresent()
    {
        var company = BuildCompany("employer-2", "Beta");
        var paged = new CompanySearchResult(
            new List<CompanySearchItem> { ToSearchItem(company, openings: 1, vacancies: 2, logoStorageKey: "logos/beta.png") },
            Page: 1,
            PageSize: 10,
            TotalCount: 1,
            TotalPages: 1,
            TotalOpenings: 1,
            TotalVacancies: 2);

        _companyRepositoryMock
            .Setup(r => r.GetAllAsync(null, null, null, null, "name", "asc", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);
        _presignedUrlServiceMock
            .Setup(s => s.CreateDownloadUrlAsync("logos/beta.png", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://cdn.example/logos/beta.png");

        var handler = new GetCompaniesQueryHandler(_unitOfWorkMock.Object, _presignedUrlServiceMock.Object);

        var result = await handler.Handle(new GetCompaniesQuery(), CancellationToken.None);

        Assert.Equal("https://cdn.example/logos/beta.png", Assert.Single(result.Items).LogoUrl);
    }
}
