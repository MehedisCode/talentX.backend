using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.AiConfiguration.Constants;
using TalentX.Application.Features.AiConfiguration.Queries.GetAiConfiguration;
using AiConfigurationEntity = TalentX.Domain.Entities.AiConfiguration;
using Xunit;

namespace TalentX.Application.Tests.Features.AiConfiguration;

public sealed class GetAiConfigurationQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAiConfigurationRepository> _aiConfigurationRepository = new();
    private readonly Mock<IConfiguration> _configuration = new();

    public GetAiConfigurationQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.AiConfigurations).Returns(_aiConfigurationRepository.Object);
    }

    [Fact]
    public async Task Handle_WhenConfigDoesNotExistAndEnvironmentKeyExists_ReturnsFallbackDtoWithEnvMask()
    {
        _aiConfigurationRepository
            .Setup(repo => repo.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConfigurationEntity?)null);

        _configuration.SetupGet(config => config["GEMINI_MODEL_NAME"]).Returns("gemini-2.5-flash");
        _configuration.SetupGet(config => config["GEMINI_API_KEY"]).Returns("environment-secret-key");

        var handler = new GetAiConfigurationQueryHandler(_unitOfWork.Object, _configuration.Object);

        var result = await handler.Handle(new GetAiConfigurationQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.ModelName.Should().Be("gemini-2.5-flash");
        result.MaskedApiKey.Should().Be(AiConfigurationConstants.EnvironmentKeyMasked);
        result.HasCustomKey.Should().BeTrue();
        result.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenConfigDoesNotExistAndNoEnvironmentKey_ReturnsDefaultModelAndNotConfigured()
    {
        _aiConfigurationRepository
            .Setup(repo => repo.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConfigurationEntity?)null);

        var handler = new GetAiConfigurationQueryHandler(_unitOfWork.Object, _configuration.Object);

        var result = await handler.Handle(new GetAiConfigurationQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.ModelName.Should().Be(AiConfigurationConstants.DefaultModelName);
        result.MaskedApiKey.Should().Be(AiConfigurationConstants.NotConfigured);
        result.HasCustomKey.Should().BeFalse();
        result.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenConfigExistsWithEncryptedKey_ReturnsConfiguredModelAndMaskedKey()
    {
        var existingConfig = new AiConfigurationEntity("gemini-1.5-pro", "encrypted-key-payload");
        _aiConfigurationRepository
            .Setup(repo => repo.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConfig);

        var handler = new GetAiConfigurationQueryHandler(_unitOfWork.Object, _configuration.Object);

        var result = await handler.Handle(new GetAiConfigurationQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.ModelName.Should().Be("gemini-1.5-pro");
        result.MaskedApiKey.Should().Be(AiConfigurationConstants.MaskedKeyPlaceholder);
        result.HasCustomKey.Should().BeTrue();
        result.UpdatedAt.Should().Be(existingConfig.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenConfigExistsWithoutEncryptedKey_ReturnsConfiguredModelAndNotConfigured()
    {
        var existingConfig = new AiConfigurationEntity("gemini-2.0-flash", null);
        _aiConfigurationRepository
            .Setup(repo => repo.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConfig);

        var handler = new GetAiConfigurationQueryHandler(_unitOfWork.Object, _configuration.Object);

        var result = await handler.Handle(new GetAiConfigurationQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.ModelName.Should().Be("gemini-2.0-flash");
        result.MaskedApiKey.Should().Be(AiConfigurationConstants.NotConfigured);
        result.HasCustomKey.Should().BeFalse();
    }
}
