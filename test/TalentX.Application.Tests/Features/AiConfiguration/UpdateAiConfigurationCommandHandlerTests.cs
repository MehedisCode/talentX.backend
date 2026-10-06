using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.AiConfiguration.Commands.UpdateAiConfiguration;
using TalentX.Application.Features.AiConfiguration.Constants;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using AiConfigurationEntity = TalentX.Domain.Entities.AiConfiguration;
using Xunit;

namespace TalentX.Application.Tests.Features.AiConfiguration;

public sealed class UpdateAiConfigurationCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAiConfigurationRepository> _aiConfigurationRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IDataProtectionProvider> _dataProtectionProvider = new();
    private readonly Mock<IDataProtector> _dataProtector = new();
    private readonly Mock<IMemoryCache> _memoryCache = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly UpdateAiConfigurationCommandHandler _handler;

    public UpdateAiConfigurationCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.AiConfigurations).Returns(_aiConfigurationRepository.Object);
        _unitOfWork.SetupGet(u => u.AuditLogs).Returns(_auditLogRepository.Object);

        _dataProtector
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns<byte[]>(bytes => bytes);

        _dataProtectionProvider
            .Setup(provider => provider.CreateProtector(AiConfigurationConstants.DataProtectionPurpose))
            .Returns(_dataProtector.Object);

        _currentUserService.SetupGet(service => service.Email).Returns("admin@talentx.ai");

        _handler = new UpdateAiConfigurationCommandHandler(
            _unitOfWork.Object,
            _dataProtectionProvider.Object,
            _memoryCache.Object,
            _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenNoExistingConfigurationAndNewKeyProvided_CreatesAndProtectsKeyAndLogsAudit()
    {
        _aiConfigurationRepository
            .Setup(repo => repo.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConfigurationEntity?)null);

        var command = new UpdateAiConfigurationCommand("gemini-2.0-flash", "test-api-key");

        AiConfigurationEntity? addedConfiguration = null;
        _aiConfigurationRepository
            .Setup(repo => repo.AddAsync(It.IsAny<AiConfigurationEntity>(), It.IsAny<CancellationToken>()))
            .Callback<AiConfigurationEntity, CancellationToken>((config, _) => addedConfiguration = config)
            .Returns(Task.CompletedTask);

        AuditLog? capturedAuditLog = null;
        _auditLogRepository
            .Setup(repo => repo.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((log, _) => capturedAuditLog = log)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.ModelName.Should().Be("gemini-2.0-flash");
        result.MaskedApiKey.Should().Be(AiConfigurationConstants.MaskedKeyPlaceholder);
        result.HasCustomKey.Should().BeTrue();

        addedConfiguration.Should().NotBeNull();
        addedConfiguration!.ModelName.Should().Be("gemini-2.0-flash");
        addedConfiguration.EncryptedApiKey.Should().NotBeNullOrWhiteSpace();

        _memoryCache.Verify(cache => cache.Remove(AiConfigurationConstants.CacheKey), Times.Once);

        capturedAuditLog.Should().NotBeNull();
        capturedAuditLog!.Actor.Should().Be("admin@talentx.ai");
        capturedAuditLog.Action.Should().Be("Updated AI configuration to model 'gemini-2.0-flash'");

        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExistingConfigurationAndMaskedKeyProvided_RetainsExistingKey()
    {
        const string existingEncryptedKey = "existing-encrypted-key-payload";
        var existingConfig = new AiConfigurationEntity("gemini-1.5-pro", existingEncryptedKey);

        _aiConfigurationRepository
            .Setup(repo => repo.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConfig);

        var command = new UpdateAiConfigurationCommand("gemini-2.0-flash", AiConfigurationConstants.MaskedKeyPlaceholder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.ModelName.Should().Be("gemini-2.0-flash");
        result.MaskedApiKey.Should().Be(AiConfigurationConstants.MaskedKeyPlaceholder);
        result.HasCustomKey.Should().BeTrue();

        existingConfig.ModelName.Should().Be("gemini-2.0-flash");
        existingConfig.EncryptedApiKey.Should().Be(existingEncryptedKey);

        _dataProtectionProvider.Verify(
            provider => provider.CreateProtector(It.IsAny<string>()),
            Times.Never);

        _aiConfigurationRepository.Verify(repo => repo.Update(existingConfig), Times.Once);
        _memoryCache.Verify(cache => cache.Remove(AiConfigurationConstants.CacheKey), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExistingConfigurationAndNewKeyProvided_UpdatesAndProtectsKey()
    {
        var existingConfig = new AiConfigurationEntity("gemini-1.5-pro", "old-encrypted-key");

        _aiConfigurationRepository
            .Setup(repo => repo.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConfig);

        var command = new UpdateAiConfigurationCommand("gemini-2.5-pro", "brand-new-secret-key");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.ModelName.Should().Be("gemini-2.5-pro");
        result.MaskedApiKey.Should().Be(AiConfigurationConstants.MaskedKeyPlaceholder);
        result.HasCustomKey.Should().BeTrue();

        existingConfig.ModelName.Should().Be("gemini-2.5-pro");
        existingConfig.EncryptedApiKey.Should().NotBe("old-encrypted-key");

        _dataProtectionProvider.Verify(
            provider => provider.CreateProtector(AiConfigurationConstants.DataProtectionPurpose),
            Times.Once);

        _aiConfigurationRepository.Verify(repo => repo.Update(existingConfig), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoExistingConfigurationAndNoKeyProvided_CreatesConfigurationWithoutKey()
    {
        _aiConfigurationRepository
            .Setup(repo => repo.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConfigurationEntity?)null);

        var command = new UpdateAiConfigurationCommand("gemini-2.0-flash", null);

        AiConfigurationEntity? addedConfiguration = null;
        _aiConfigurationRepository
            .Setup(repo => repo.AddAsync(It.IsAny<AiConfigurationEntity>(), It.IsAny<CancellationToken>()))
            .Callback<AiConfigurationEntity, CancellationToken>((config, _) => addedConfiguration = config)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.ModelName.Should().Be("gemini-2.0-flash");
        result.MaskedApiKey.Should().Be(AiConfigurationConstants.NotConfigured);
        result.HasCustomKey.Should().BeFalse();

        addedConfiguration.Should().NotBeNull();
        addedConfiguration!.EncryptedApiKey.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenCurrentUserEmailIsNull_FallsBackToAdminInAuditLog()
    {
        _currentUserService.SetupGet(service => service.Email).Returns((string?)null);

        _aiConfigurationRepository
            .Setup(repo => repo.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConfigurationEntity?)null);

        var command = new UpdateAiConfigurationCommand("gemini-2.0-flash", null);

        AuditLog? capturedAuditLog = null;
        _auditLogRepository
            .Setup(repo => repo.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((log, _) => capturedAuditLog = log)
            .Returns(Task.CompletedTask);

        await _handler.Handle(command, CancellationToken.None);

        capturedAuditLog.Should().NotBeNull();
        capturedAuditLog!.Actor.Should().Be("admin");
    }
}
