using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Identity.Models;
using TalentX.Infrastructure.Identity.Seed;
using Xunit;

namespace TalentX.Infrastructure.Tests.Identity.Seed;

public sealed class IdentitySeederTests
{
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITalentRepository> _talentRepositoryMock;
    private readonly Mock<IResumeRepository> _resumeRepositoryMock;
    private readonly ServiceCollection _services;

    public IdentitySeederTests()
    {
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            new Mock<IRoleStore<IdentityRole>>().Object,
            null!, null!, null!, null!);

        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        _userRepositoryMock = new Mock<IUserRepository>();
        _talentRepositoryMock = new Mock<ITalentRepository>();
        _resumeRepositoryMock = new Mock<IResumeRepository>();

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Talents).Returns(_talentRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Resumes).Returns(_resumeRepositoryMock.Object);

        _services = new ServiceCollection();
        _services.AddScoped(_ => _roleManagerMock.Object);
        _services.AddScoped(_ => _userManagerMock.Object);
        _services.AddScoped(_ => _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task SeedAsync_WhenAdminCredentialsProvided_ShouldSeedAdminRoleAndSuperAdminUser()
    {
        // Arrange
        const string adminEmail = "superadmin@ait.com";
        const string adminPass = "SuperAdmin@123";

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ADMIN_EMAIL"] = adminEmail,
                ["ADMIN_PASS"] = adminPass
            })
            .Build();

        _services.AddSingleton<IConfiguration>(config);
        var serviceProvider = _services.BuildServiceProvider();

        _roleManagerMock.Setup(r => r.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(u => u.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        // Act
        await IdentitySeeder.SeedAsync(serviceProvider);

        // Assert
        _roleManagerMock.Verify(r => r.CreateAsync(It.Is<IdentityRole>(role => role.Name == UserRole.Admin.ToString())), Times.Once);
        _userManagerMock.Verify(u => u.CreateAsync(It.Is<ApplicationUser>(u => u.Email == adminEmail), adminPass), Times.Once);
        _userManagerMock.Verify(u => u.AddToRoleAsync(It.Is<ApplicationUser>(u => u.Email == adminEmail), UserRole.Admin.ToString()), Times.Once);
        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == adminEmail && u.Role == UserRole.Admin), default), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.AtLeastOnce);
    }
}
