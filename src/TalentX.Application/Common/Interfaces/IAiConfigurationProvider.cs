namespace TalentX.Application.Common.Interfaces;

public interface IAiConfigurationProvider
{
    Task<(string ModelName, string ApiKey)> GetConfigAsync(CancellationToken cancellationToken = default);
}
