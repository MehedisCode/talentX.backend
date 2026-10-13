using System.Net;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Infrastructure.Ai;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai;

public class GeminiExtractionServiceTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();
        public int CallCount { get; private set; }

        public void Enqueue(HttpResponseMessage response) => _responses.Enqueue(response);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            if (_responses.Count > 0)
            {
                return Task.FromResult(_responses.Dequeue());
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }

    private static byte[] CreateSampleDocx(string text)
    {
        using var mem = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text(text)))));
            mainPart.Document.Save();
        }
        return mem.ToArray();
    }

    private static Mock<IUploadedFile> CreateMockFile(string text)
    {
        var bytes = CreateSampleDocx(text);
        var mock = new Mock<IUploadedFile>();
        mock.Setup(f => f.FileName).Returns("resume.docx");
        mock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(bytes));
        return mock;
    }

    private static HttpResponseMessage CreateGeminiSuccessResponse(string jsonContent)
    {
        var escapedJson = jsonContent.Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
        var geminiResponseJson = $$"""
        {
            "candidates": [
                {
                    "content": {
                        "parts": [
                            {
                                "text": "{{escapedJson}}"
                            }
                        ]
                    }
                }
            ]
        }
        """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(geminiResponseJson, Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task ExtractAsync_WhenFirstAttemptFailsWith503_RetriesAndSucceedsOnSecondAttempt()
    {
        var fakeHandler = new FakeHttpMessageHandler();
        fakeHandler.Enqueue(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("Service Unavailable")
        });

        const string validJson = """
        {
            "fullName": "Jane Doe",
            "email": "jane@example.com",
            "technicalSkills": ["C#", ".NET"]
        }
        """;
        fakeHandler.Enqueue(CreateGeminiSuccessResponse(validJson));

        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("https://generativelanguage.googleapis.com") };
        var configProviderMock = new Mock<IAiConfigurationProvider>();
        configProviderMock.Setup(c => c.GetConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(("gemini-2.5-flash", "test-key"));

        var config = new ConfigurationBuilder().Build();
        var service = new GeminiExtractionService(
            httpClient,
            configProviderMock.Object,
            config,
            NullLogger<GeminiExtractionService>.Instance);

        var file = CreateMockFile("Jane Doe - Senior .NET Developer with 8 years of experience in C# and .NET Core.");

        var result = await service.ExtractAsync(file.Object);

        result.Should().NotBeNull();
        result.FullName.Should().Be("Jane Doe");
        result.TechnicalSkills.Should().Contain("C#");
        fakeHandler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task ExtractAsync_DefaultOverload_ExtractsFullProfileFields()
    {
        var fakeHandler = new FakeHttpMessageHandler();
        const string validJson = """
        {
            "fullName": "Jane Doe",
            "tagline": "Senior .NET Architect",
            "shortBio": "Over 10 years of experience building distributed systems.",
            "email": "jane@example.com",
            "phone": "+1234567890",
            "location": "New York, NY",
            "linkedInUrl": "https://linkedin.com/in/janedoe",
            "portfolioUrls": ["https://janedoe.dev"],
            "technicalSkills": ["C#", ".NET 10"],
            "softSkills": ["Leadership"],
            "toolsAndPlatforms": ["Docker", "AWS"],
            "workExperience": [
                {
                    "companyName": "Tech Corp",
                    "jobTitle": "Lead Developer",
                    "startDate": "2020-01",
                    "endDate": "Present",
                    "isCurrent": true
                }
            ],
            "education": [
                {
                    "institution": "MIT",
                    "degree": "B.S. Computer Science",
                    "startDate": "2012",
                    "endDate": "2016"
                }
            ],
            "projects": [
                {
                    "projectName": "Cloud Marketplace",
                    "role": "Lead",
                    "projectType": "Professional",
                    "startDate": "2022-01",
                    "endDate": "2023-01",
                    "isOngoing": false,
                    "description": "High-throughput marketplace",
                    "technologiesUsed": ["C#", ".NET"],
                    "projectUrl": "https://github.com/example/proj"
                }
            ]
        }
        """;
        fakeHandler.Enqueue(CreateGeminiSuccessResponse(validJson));

        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("https://generativelanguage.googleapis.com") };
        var configProviderMock = new Mock<IAiConfigurationProvider>();
        configProviderMock.Setup(c => c.GetConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(("gemini-2.5-flash", "test-key"));

        var config = new ConfigurationBuilder().Build();
        var service = new GeminiExtractionService(
            httpClient,
            configProviderMock.Object,
            config,
            NullLogger<GeminiExtractionService>.Instance);

        var file = CreateMockFile("Jane Doe - Senior .NET Architect with 10 years experience.");

        var result = await service.ExtractAsync(file.Object);

        result.Should().NotBeNull();
        result.FullName.Should().Be("Jane Doe");
        result.Tagline.Should().Be("Senior .NET Architect");
        result.ShortBio.Should().Be("Over 10 years of experience building distributed systems.");
        result.Projects.Should().HaveCount(1);
        result.Projects[0].ProjectName.Should().Be("Cloud Marketplace");
    }

    [Fact]
    public async Task ExtractAsync_WhenFirstAttemptReturnsEmptyData_ThrowsImmediately()
    {
        var fakeHandler = new FakeHttpMessageHandler();

        const string emptyJson = """
        {
            "fullName": "",
            "workExperience": [],
            "education": [],
            "technicalSkills": [],
            "softSkills": [],
            "toolsAndPlatforms": []
        }
        """;
        var response = CreateGeminiSuccessResponse(emptyJson);
        fakeHandler.Enqueue(response);

        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("https://generativelanguage.googleapis.com") };
        var configProviderMock = new Mock<IAiConfigurationProvider>();
        configProviderMock.Setup(c => c.GetConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(("gemini-2.5-flash", "test-key"));

        var config = new ConfigurationBuilder().Build();
        var service = new GeminiExtractionService(
            httpClient,
            configProviderMock.Object,
            config,
            NullLogger<GeminiExtractionService>.Instance);

        var file = CreateMockFile("John Smith - Full Stack Developer with extensive professional experience. Extensive background with TypeScript, React, and web technologies. Over 10 years of work history building enterprise applications. Skilled in modern development practices and agile methodologies. Proven track record of delivering high-quality software solutions for Fortune 500 companies. Expert in front-end and back-end development.");

        var act = () => service.ExtractAsync(file.Object);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*incomplete or empty data*");
        fakeHandler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ExtractAsync_WhenAllAttemptsFail_ThrowsException()
    {
        var fakeHandler = new FakeHttpMessageHandler();
        fakeHandler.Enqueue(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        fakeHandler.Enqueue(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        fakeHandler.Enqueue(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("https://generativelanguage.googleapis.com") };
        var configProviderMock = new Mock<IAiConfigurationProvider>();
        configProviderMock.Setup(c => c.GetConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(("gemini-2.5-flash", "test-key"));

        var config = new ConfigurationBuilder().Build();
        var service = new GeminiExtractionService(
            httpClient,
            configProviderMock.Object,
            config,
            NullLogger<GeminiExtractionService>.Instance);

        var file = CreateMockFile("Jane Doe - Senior .NET Developer with 8 years of experience in C# and .NET Core.");

        var act = () => service.ExtractAsync(file.Object);

        await act.Should().ThrowAsync<Exception>();
        fakeHandler.CallCount.Should().Be(3);
    }
}
