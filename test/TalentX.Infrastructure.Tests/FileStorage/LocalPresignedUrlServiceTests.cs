using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TalentX.Infrastructure.FileStorage;
using Xunit;

namespace TalentX.Infrastructure.Tests.FileStorage;

public sealed class LocalPresignedUrlServiceTests
{
    [Fact]
    public async Task CreateUploadUrlAsync_UsesAppBaseUrlWhenConfigured()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "App:BaseUrl", "https://api.talentx.local:5001/" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var service = new LocalPresignedUrlService(configuration);
        var storageKey = "resumes/candidate-resume.pdf";
        var contentType = "application/pdf";
        var expiry = TimeSpan.FromMinutes(10);

        var startTime = DateTimeOffset.UtcNow;
        var result = await service.CreateUploadUrlAsync(storageKey, contentType, expiry);
        var endTime = DateTimeOffset.UtcNow;

        result.Should().NotBeNull();
        result.UploadUrl.Should().Be("https://api.talentx.local:5001/api/v1/files/upload?key=resumes%2Fcandidate-resume.pdf");
        result.StorageKey.Should().Be(storageKey);
        result.HttpMethod.Should().Be("PUT");
        result.ExpiresAt.Should().BeOnOrAfter(startTime.Add(expiry)).And.BeOnOrBefore(endTime.Add(expiry));
    }

    [Fact]
    public async Task CreateUploadUrlAsync_FallsBackToAspNetCoreUrlsWhenAppBaseUrlNotConfigured()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ASPNETCORE_URLS", "http://localhost:5000;https://localhost:5001" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var service = new LocalPresignedUrlService(configuration);
        var storageKey = "resumes/sample.pdf";

        var result = await service.CreateUploadUrlAsync(storageKey, "application/pdf", TimeSpan.FromMinutes(5));

        result.UploadUrl.Should().Be("http://localhost:5000/api/v1/files/upload?key=resumes%2Fsample.pdf");
    }

    [Fact]
    public async Task CreateUploadUrlAsync_FallsBackToDefaultPortWhenNoConfigurationProvided()
    {
        var emptyConfiguration = new ConfigurationBuilder().Build();
        var service = new LocalPresignedUrlService(emptyConfiguration);
        var storageKey = "sample.pdf";

        var result = await service.CreateUploadUrlAsync(storageKey, "application/pdf", TimeSpan.FromMinutes(5));

        result.UploadUrl.Should().Be("http://localhost:5035/api/v1/files/upload?key=sample.pdf");
    }

    [Fact]
    public async Task CreateDownloadUrlAsync_GeneratesLocalStreamEndpointUrl()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "App:BaseUrl", "https://api.talentx.local:5001" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var service = new LocalPresignedUrlService(configuration);
        var storageKey = "resumes/folder name/candidate.pdf";

        var result = await service.CreateDownloadUrlAsync(storageKey, TimeSpan.FromHours(1));

        result.Should().Be("https://api.talentx.local:5001/api/v1/files/stream?key=resumes%2Ffolder%20name%2Fcandidate.pdf");
    }
}
