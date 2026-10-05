namespace TalentX.Infrastructure.Tests.Services;

using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.AiConfiguration.Constants;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Services;
using Xunit;

public sealed class AiConfigurationProviderTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAiConfigurationRepository> _aiConfigurationRepository = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ILogger<AiConfigurationProvider>> _logger = new();
    private readonly IDataProtectionProvider _dataProtectionProvider = new EphemeralDataProtectionProvider();
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

    public AiConfigurationProviderTests()
    {
        _unitOfWork.SetupGet(unit => unit.AiConfigurations).Returns(_aiConfigurationRepository.Object);
    }

    [Fact]
    public async Task GetConfigAsync_WhenDatabaseRowIsNull_FallsBackToConfiguration()
    {
        _aiConfigurationRepository
            .Setup(repository => repository.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConfiguration?)null);

        _configuration.SetupGet(config => config["GEMINI_MODEL_NAME"]).Returns("gemini-custom-model");
        _configuration.SetupGet(config => config["GEMINI_API_KEY"]).Returns("custom-environment-api-key");

        var provider = new AiConfigurationProvider(
            _unitOfWork.Object,
            _dataProtectionProvider,
            _memoryCache,
            _configuration.Object,
            _logger.Object);

        var (modelName, apiKey) = await provider.GetConfigAsync();

        modelName.Should().Be("gemini-custom-model");
        apiKey.Should().Be("custom-environment-api-key");
    }

    [Fact]
    public async Task GetConfigAsync_WhenEntityExistsWithEncryptedKey_DecryptsAndReturnsDbConfig()
    {
        const string rawApiKey = "super-secret-api-key-12345";
        var protector = _dataProtectionProvider.CreateProtector(AiConfigurationConstants.DataProtectionPurpose);
        var encryptedKey = protector.Protect(rawApiKey);

        var existingEntity = new AiConfiguration("gemini-1.5-pro", encryptedKey);
        _aiConfigurationRepository
            .Setup(repository => repository.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        var provider = new AiConfigurationProvider(
            _unitOfWork.Object,
            _dataProtectionProvider,
            _memoryCache,
            _configuration.Object,
            _logger.Object);

        var (modelName, apiKey) = await provider.GetConfigAsync();

        modelName.Should().Be("gemini-1.5-pro");
        apiKey.Should().Be(rawApiKey);
    }

    [Fact]
    public async Task GetConfigAsync_WhenCalledMultipleTimes_UsesCacheAndAvoidsSecondDatabaseCall()
    {
        var existingEntity = new AiConfiguration("gemini-2.0-flash", null);
        _aiConfigurationRepository
            .Setup(repository => repository.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _configuration.SetupGet(config => config["GEMINI_API_KEY"]).Returns("cached-env-key");

        var provider = new AiConfigurationProvider(
            _unitOfWork.Object,
            _dataProtectionProvider,
            _memoryCache,
            _configuration.Object,
            _logger.Object);

        var firstResult = await provider.GetConfigAsync();
        var secondResult = await provider.GetConfigAsync();

        firstResult.Should().Be(secondResult);
        firstResult.ModelName.Should().Be("gemini-2.0-flash");
        firstResult.ApiKey.Should().Be("cached-env-key");

        _aiConfigurationRepository.Verify(
            repository => repository.GetAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConfigAsync_WhenEntityExistsWithoutKey_FallsBackToEnvironmentKey()
    {
        var existingEntity = new AiConfiguration("gemini-2.5-flash", null);
        _aiConfigurationRepository
            .Setup(repository => repository.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _configuration.SetupGet(config => config["GEMINI_API_KEY"]).Returns("env-key-for-db-model");

        var provider = new AiConfigurationProvider(
            _unitOfWork.Object,
            _dataProtectionProvider,
            _memoryCache,
            _configuration.Object,
            _logger.Object);

        var (modelName, apiKey) = await provider.GetConfigAsync();

        modelName.Should().Be("gemini-2.5-flash");
        apiKey.Should().Be("env-key-for-db-model");
    }

    [Fact]
    public async Task GetConfigAsync_WhenDecryptionFails_LogsErrorAndFallsBackToEnvironmentKey()
    {
        var existingEntity = new AiConfiguration("gemini-1.5-flash", "malformed-unparseable-encrypted-data");
        _aiConfigurationRepository
            .Setup(repository => repository.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _configuration.SetupGet(config => config["GEMINI_API_KEY"]).Returns("fallback-env-key");

        var provider = new AiConfigurationProvider(
            _unitOfWork.Object,
            _dataProtectionProvider,
            _memoryCache,
            _configuration.Object,
            _logger.Object);

        var (modelName, apiKey) = await provider.GetConfigAsync();

        modelName.Should().Be("gemini-1.5-flash");
        apiKey.Should().Be("fallback-env-key");
    }

    [Fact]
    public async Task GetConfigAsync_WhenDatabaseFails_LogsErrorAndFallsBackToConfiguration()
    {
        _aiConfigurationRepository
            .Setup(repository => repository.GetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection timeout"));

        _configuration.SetupGet(config => config["GEMINI_MODEL_NAME"]).Returns("fallback-env-model");
        _configuration.SetupGet(config => config["GEMINI_API_KEY"]).Returns("fallback-env-key");

        var provider = new AiConfigurationProvider(
            _unitOfWork.Object,
            _dataProtectionProvider,
            _memoryCache,
            _configuration.Object,
            _logger.Object);

        var (modelName, apiKey) = await provider.GetConfigAsync();

        modelName.Should().Be("fallback-env-model");
        apiKey.Should().Be("fallback-env-key");
    }

    [Fact]
    public async Task GetConfigAsync_WhenNoDatabaseAndNoEnvironmentConfiguration_ReturnsDefaultModelAndEmptyKey()
    {
        _aiConfigurationRepository
            .Setup(repository => repository.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConfiguration?)null);

        var provider = new AiConfigurationProvider(
            _unitOfWork.Object,
            _dataProtectionProvider,
            _memoryCache,
            _configuration.Object,
            _logger.Object);

        var (modelName, apiKey) = await provider.GetConfigAsync();

        modelName.Should().Be(AiConfigurationConstants.DefaultModelName);
        apiKey.Should().BeEmpty();
    }
}
