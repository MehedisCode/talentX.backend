namespace TalentX.Application.Features.AiConfiguration.Commands.UpdateAiConfiguration;

using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.AiConfiguration.Constants;
using TalentX.Application.Features.AiConfiguration.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

public sealed class UpdateAiConfigurationCommandHandler : IRequestHandler<UpdateAiConfigurationCommand, AiConfigurationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly ICurrentUserService _currentUserService;

    public UpdateAiConfigurationCommandHandler(
        IUnitOfWork unitOfWork,
        IDataProtectionProvider dataProtectionProvider,
        IMemoryCache memoryCache,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _dataProtectionProvider = dataProtectionProvider;
        _memoryCache = memoryCache;
        _currentUserService = currentUserService;
    }

    public async Task<AiConfigurationDto> Handle(UpdateAiConfigurationCommand request, CancellationToken cancellationToken)
    {
        var existingConfiguration = await _unitOfWork.AiConfigurations.GetAsync(cancellationToken);
        string? encryptedApiKey = null;
        var isNewKey = !string.IsNullOrWhiteSpace(request.ApiKey)
            && !request.ApiKey.StartsWith(AiConfigurationConstants.MaskedKeyPlaceholder, StringComparison.Ordinal);

        if (isNewKey)
        {
            var dataProtector = _dataProtectionProvider.CreateProtector(AiConfigurationConstants.DataProtectionPurpose);
            encryptedApiKey = dataProtector.Protect(request.ApiKey!);
        }
        else if (existingConfiguration is not null)
        {
            encryptedApiKey = existingConfiguration.EncryptedApiKey;
        }

        if (existingConfiguration is null)
        {
            existingConfiguration = new AiConfiguration(request.ModelName, encryptedApiKey);
            await _unitOfWork.AiConfigurations.AddAsync(existingConfiguration, cancellationToken);
        }
        else
        {
            existingConfiguration.Update(request.ModelName, encryptedApiKey);
            _unitOfWork.AiConfigurations.Update(existingConfiguration);
        }

        _memoryCache.Remove(AiConfigurationConstants.CacheKey);

        var actorEmail = _currentUserService.Email ?? "admin";
        await _unitOfWork.AuditLogs.AddAsync(
            new AuditLog(actorEmail, $"Updated AI configuration to model '{request.ModelName}'"),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var hasKey = !string.IsNullOrWhiteSpace(existingConfiguration.EncryptedApiKey);
        var maskedApiKey = hasKey
            ? AiConfigurationConstants.MaskedKeyPlaceholder
            : AiConfigurationConstants.NotConfigured;

        return new AiConfigurationDto(
            existingConfiguration.ModelName,
            maskedApiKey,
            hasKey,
            existingConfiguration.UpdatedAt ?? existingConfiguration.CreatedAt);
    }
}
