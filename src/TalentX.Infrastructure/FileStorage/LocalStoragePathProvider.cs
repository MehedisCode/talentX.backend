using Microsoft.Extensions.Configuration;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.FileStorage;

public sealed class LocalStoragePathProvider : ILocalStoragePathProvider
{
    private readonly string _basePath;

    public LocalStoragePathProvider(IConfiguration configuration)
    {
        var configuredBasePath = Environment.GetEnvironmentVariable("FILE_STORAGE_LOCAL_BASE_PATH")
            ?? configuration["FILE_STORAGE_LOCAL_BASE_PATH"]
            ?? configuration["FileStorage:LocalBasePath"];

        _basePath = string.IsNullOrWhiteSpace(configuredBasePath)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads")
            : (Path.IsPathRooted(configuredBasePath)
                ? configuredBasePath
                : Path.Combine(AppContext.BaseDirectory, configuredBasePath));
    }

    public LocalStoragePathProvider(string basePath)
    {
        _basePath = basePath;
    }

    public string GetBasePath() => _basePath;
}
