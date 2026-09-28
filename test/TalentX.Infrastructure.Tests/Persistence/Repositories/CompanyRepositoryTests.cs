using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class CompanyRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Company MakeCompany(string employerId, string name, string? industry = null, string? location = null, string? size = null)
    {
        var company = new Company(employerId, name);
        company.UpdateProfile(name, $"{name} description", industry, size, null, location, null, null, null);
        return company;
    }

    private static Job MakeJob(string employerId, int vacancies, DateTime expireAt)
    {
        var job = Job.Create(
            employerId,
            $"Title {Guid.NewGuid()}",
            "Company",
            "Description",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.OnSite,
            new List<string> { "C#" },
            vacancies,
            expireAt);
        job.MarkAsPublished();
        return job;
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsCompanyCaseInsensitively()
    {
        await using var context = NewContext();
        context.Companies.Add(MakeCompany("emp-1", "Acme Corp"));
        await context.SaveChangesAsync();
        var repository = new CompanyRepository(context);

        var found = await repository.GetByNameAsync("  ACME CORP  ");
        found.Should().NotBeNull();
        found!.Name.Should().Be("Acme Corp");

        (await repository.GetByNameAsync("Beta Ltd")).Should().BeNull();
        (await repository.GetByNameAsync("   ")).Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsNameAscending_WithOpeningsAndVacanciesCounts()
    {
        await using var context = NewContext();
        context.Companies.Add(MakeCompany("emp-b", "Beta Ltd"));
        context.Companies.Add(MakeCompany("emp-a", "Acme Corp"));
        context.Jobs.Add(MakeJob("emp-a", vacancies: 2, DateTime.UtcNow.AddDays(30)));
        context.Jobs.Add(MakeJob("emp-a", vacancies: 3, DateTime.UtcNow.AddDays(30)));
        await context.SaveChangesAsync();
        var repository = new CompanyRepository(context);

        var result = await repository.GetAllAsync(null, null, null, null, "name", "asc", 1, 10);

        result.TotalCount.Should().Be(2);
        result.Items[0].Name.Should().Be("Acme Corp");
        result.Items[0].OpeningsCount.Should().Be(2);
        result.Items[0].VacanciesCount.Should().Be(5);
        result.Items[1].Name.Should().Be("Beta Ltd");
        result.Items[1].OpeningsCount.Should().Be(0);
        result.Items[1].VacanciesCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesClosedExpiredDeactivatedAndDeletedJobs()
    {
        await using var context = NewContext();
        context.Companies.Add(MakeCompany("emp-1", "Acme Corp"));

        var active = MakeJob("emp-1", vacancies: 4, DateTime.UtcNow.AddDays(30));

        var closed = MakeJob("emp-1", vacancies: 10, DateTime.UtcNow.AddDays(30));
        closed.MarkAsClosed();

        var expired = MakeJob("emp-1", vacancies: 10, DateTime.UtcNow.AddDays(-1));

        var deactivated = MakeJob("emp-1", vacancies: 10, DateTime.UtcNow.AddDays(30));
        deactivated.Deactivate(DeactivationReason.Other, "test");

        var draft = Job.Create(
            "emp-1", "Draft", "Company", "Description",
            EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka",
            WorkArrangement.OnSite, new List<string> { "C#" }, 10,
            DateTime.UtcNow.AddDays(30));

        var deleted = MakeJob("emp-1", vacancies: 10, DateTime.UtcNow.AddDays(30));
        deleted.SoftDelete();

        context.Jobs.AddRange(active, closed, expired, deactivated, draft, deleted);
        await context.SaveChangesAsync();
        var repository = new CompanyRepository(context);

        var result = await repository.GetAllAsync(null, null, null, null, "name", "asc", 1, 10);

        var item = result.Items.Should().ContainSingle().Subject;
        item.OpeningsCount.Should().Be(1);
        item.VacanciesCount.Should().Be(4);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByKeywordIndustryLocationAndSize()
    {
        await using var context = NewContext();
        context.Companies.Add(MakeCompany("emp-1", "Acme Corp", industry: "Software", location: "Dhaka", size: "11-50"));
        context.Companies.Add(MakeCompany("emp-2", "Beta Bank", industry: "Finance & Banking", location: "Chittagong", size: "501-1000"));
        await context.SaveChangesAsync();
        var repository = new CompanyRepository(context);

        (await repository.GetAllAsync("acme", null, null, null, "name", "asc", 1, 10))
            .Items.Should().ContainSingle().Which.Name.Should().Be("Acme Corp");

        (await repository.GetAllAsync(null, "banking", null, null, "name", "asc", 1, 10))
            .Items.Should().ContainSingle().Which.Name.Should().Be("Beta Bank");

        (await repository.GetAllAsync(null, null, "chittagong", null, "name", "asc", 1, 10))
            .Items.Should().ContainSingle().Which.Name.Should().Be("Beta Bank");

        (await repository.GetAllAsync(null, null, null, "11-50", "name", "asc", 1, 10))
            .Items.Should().ContainSingle().Which.Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetAllAsync_KeywordMatchesNameLocationAndIndustry()
    {
        await using var context = NewContext();
        context.Companies.Add(MakeCompany("emp-1", "Acme Corp", industry: "Software", location: "Dhaka", size: "11-50"));
        context.Companies.Add(MakeCompany("emp-2", "Beta Bank", industry: "Finance & Banking", location: "Chittagong", size: "501-1000"));
        await context.SaveChangesAsync();
        var repository = new CompanyRepository(context);

        (await repository.GetAllAsync("acme", null, null, null, "name", "asc", 1, 10))
            .Items.Should().ContainSingle().Which.Name.Should().Be("Acme Corp");

        // Location match.
        (await repository.GetAllAsync("chittagong", null, null, null, "name", "asc", 1, 10))
            .Items.Should().ContainSingle().Which.Name.Should().Be("Beta Bank");

        // Industry match.
        (await repository.GetAllAsync("banking", null, null, null, "name", "asc", 1, 10))
            .Items.Should().ContainSingle().Which.Name.Should().Be("Beta Bank");

        // Keyword combines with the other filters.
        (await repository.GetAllAsync("dhaka", "Software", null, null, "name", "asc", 1, 10))
            .Items.Should().ContainSingle().Which.Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetAllAsync_Paginates_AndSortsByVacanciesDesc()
    {
        await using var context = NewContext();
        context.Companies.Add(MakeCompany("emp-1", "Acme Corp"));
        context.Companies.Add(MakeCompany("emp-2", "Beta Ltd"));
        context.Companies.Add(MakeCompany("emp-3", "Gamma Inc"));
        context.Jobs.Add(MakeJob("emp-2", vacancies: 9, DateTime.UtcNow.AddDays(30)));
        context.Jobs.Add(MakeJob("emp-3", vacancies: 2, DateTime.UtcNow.AddDays(30)));
        await context.SaveChangesAsync();
        var repository = new CompanyRepository(context);

        var sorted = await repository.GetAllAsync(null, null, null, null, "vacancies", "desc", 1, 10);
        sorted.Items.Select(i => i.Name).Should().ContainInOrder("Beta Ltd", "Gamma Inc", "Acme Corp");

        var page2 = await repository.GetAllAsync(null, null, null, null, "name", "asc", 2, 1);
        page2.TotalCount.Should().Be(3);
        page2.Items.Should().ContainSingle().Which.Name.Should().Be("Beta Ltd");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsFilteredSetTotals_StableAcrossSortAndPage()
    {
        await using var context = NewContext();
        context.Companies.Add(MakeCompany("emp-1", "Acme Corp"));
        context.Companies.Add(MakeCompany("emp-2", "Beta Ltd"));
        context.Companies.Add(MakeCompany("emp-3", "Gamma Inc"));
        context.Jobs.Add(MakeJob("emp-1", vacancies: 2, DateTime.UtcNow.AddDays(30)));
        context.Jobs.Add(MakeJob("emp-1", vacancies: 3, DateTime.UtcNow.AddDays(30)));
        context.Jobs.Add(MakeJob("emp-3", vacancies: 9, DateTime.UtcNow.AddDays(30)));
        await context.SaveChangesAsync();
        var repository = new CompanyRepository(context);

        // Full set: 3 openings, 14 vacancies — identical on every sort and page.
        var byName = await repository.GetAllAsync(null, null, null, null, "name", "asc", 1, 10);
        byName.TotalOpenings.Should().Be(3);
        byName.TotalVacancies.Should().Be(14);

        var newestPage2 = await repository.GetAllAsync(null, null, null, null, "newest", "desc", 2, 1);
        newestPage2.TotalCount.Should().Be(3);
        newestPage2.TotalPages.Should().Be(3);
        newestPage2.Items.Should().ContainSingle();
        newestPage2.TotalOpenings.Should().Be(3);
        newestPage2.TotalVacancies.Should().Be(14);

        var byVacancies = await repository.GetAllAsync(null, null, null, null, "vacancies", "desc", 1, 10);
        byVacancies.TotalOpenings.Should().Be(3);
        byVacancies.TotalVacancies.Should().Be(14);

        // Filtered set: only Acme's jobs contribute.
        var filtered = await repository.GetAllAsync("acme", null, null, null, "name", "asc", 1, 10);
        filtered.TotalCount.Should().Be(1);
        filtered.TotalOpenings.Should().Be(2);
        filtered.TotalVacancies.Should().Be(5);
    }
}
