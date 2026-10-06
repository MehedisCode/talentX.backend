namespace TalentX.Infrastructure.Services;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.AiConfiguration.Constants;

public sealed class AiConfigurationProvider : IAiConfigurationProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiConfigurationProvider> _logger;

    public AiConfigurationProvider(
        IUnitOfWork unitOfWork,
        IDataProtectionProvider dataProtectionProvider,
        IMemoryCache memoryCache,
        IConfiguration configuration,
        ILogger<AiConfigurationProvider> logger)
    {
        _unitOfWork = unitOfWork;
        _dataProtectionProvider = dataProtectionProvider;
        _memoryCache = memoryCache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(string ModelName, string ApiKey)> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var config = await _memoryCache.GetOrCreateAsync(
            AiConfigurationConstants.CacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;

                try
                {
                    var entity = await _unitOfWork.AiConfigurations.GetAsync(cancellationToken);
                    if (entity is not null)
                    {
                        string resolvedApiKey = string.Empty;
                        if (!string.IsNullOrWhiteSpace(entity.EncryptedApiKey))
                        {
                            try
                            {
                                var protector = _dataProtectionProvider.CreateProtector(AiConfigurationConstants.DataProtectionPurpose);
                                resolvedApiKey = protector.Unprotect(entity.EncryptedApiKey);
                            }
                            catch (Exception exception)
                            {
                                _logger.LogError(exception, "Failed to decrypt stored AI configuration API key. Falling back to environment.");
                            }
                        }

                        if (string.IsNullOrWhiteSpace(resolvedApiKey))
                        {
                            resolvedApiKey = _configuration["GEMINI_API_KEY"]
                                ?? _configuration["Gemini:ApiKey"]
                                ?? string.Empty;
                        }

                        return (entity.ModelName, resolvedApiKey);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to load AI configuration from database. Falling back to environment.");
                }

                var fallbackModel = _configuration["GEMINI_MODEL_NAME"]
                    ?? _configuration["Gemini:ModelName"]
                    ?? AiConfigurationConstants.DefaultModelName;
                var fallbackApiKey = _configuration["GEMINI_API_KEY"]
                    ?? _configuration["Gemini:ApiKey"]
                    ?? string.Empty;

                return (fallbackModel, fallbackApiKey);
            });

        return config;
    }
}
