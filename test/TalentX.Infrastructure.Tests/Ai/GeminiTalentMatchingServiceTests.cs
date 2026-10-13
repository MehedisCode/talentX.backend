using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.TalentMatching.DTOs;
using TalentX.Infrastructure.Ai;
using TalentX.Infrastructure.Ai.Prompts;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai;

public class GeminiTalentMatchingServiceTests
{
    private const string ModelName = "gemini-test";

    private readonly StubHttpMessageHandler _handler = new();
    private GeminiTalentMatchingService _service;

    public GeminiTalentMatchingServiceTests()
    {
        _service = CreateService(_handler);
    }

    private static GeminiTalentMatchingService CreateService(
        StubHttpMessageHandler handler,
        Dictionary<string, string?>? settings = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["GEMINI_BASE_URL"] = "https://unit.test"
        };
        if (settings is not null)
        {
            foreach (var pair in settings)
            {
                values[pair.Key] = pair.Value;
            }
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var httpClient = new HttpClient(handler);
        return new GeminiTalentMatchingService(
            httpClient,
            new StubAiConfigurationProvider(ModelName, "test-key"),
            configuration,
            new TalentMatchingPromptFactory(new ITalentMatchingPrompt[] { new V1(), new V2() }),
            NullLogger<GeminiTalentMatchingService>.Instance);
    }

    private static TalentMatchingRequest SampleRequest()
    {
        return new TalentMatchingRequest
        {
            Job = new JobProfileForMatching
            {
                JobId = Guid.NewGuid(),
                Title = "Backend Engineer",
                CompanyName = "Acme",
                Description = "Build distributed systems.",
                RequiredSkills = new List<string> { "C#" },
                ExperienceLevel = "SeniorLevel",
                Location = "Remote",
                WorkArrangement = "FullRemote"
            },
            Candidates = new List<TalentProfileForMatching>
            {
                new() { TalentUserId = "talent-1", Name = "Ada Lovelace", TechnicalSkills = new List<string> { "C#" } },
                new() { TalentUserId = "talent-2", Name = "Grace Hopper", TechnicalSkills = new List<string> { "COBOL" } }
            }
        };
    }

    private static HttpResponseMessage OkGemini(string innerJson)
    {
        var envelope = new
        {
            candidates = new[]
            {
                new { content = new { parts = new[] { new { text = innerJson } } } }
            }
        };

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(envelope), Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task MatchAsync_DefaultConfiguration_SendsDeterministicGenerationConfig()
    {
        _handler.Enqueue(OkGemini("""{"matches":[{"talentUserId":"talent-1","score":80,"explanation":"ok"},{"talentUserId":"talent-2","score":40,"explanation":"weak"}]}"""));

        await _service.MatchAsync(SampleRequest());

        var body = JsonDocument.Parse(_handler.RequestBodies.Single());
        var generationConfig = body.RootElement.GetProperty("generationConfig");

        generationConfig.GetProperty("temperature").GetDouble().Should().Be(0.0);
        generationConfig.GetProperty("top_p").GetDouble().Should().Be(1.0);
        generationConfig.GetProperty("seed").GetInt32().Should().Be(42);
        generationConfig.GetProperty("max_output_tokens").GetInt32().Should().Be(4096);
        generationConfig.GetProperty("response_mime_type").GetString().Should().Be("application/json");
        generationConfig.GetProperty("response_schema").GetProperty("properties")
            .GetProperty("matches").GetProperty("type").GetString().Should().Be("ARRAY");
        generationConfig.GetProperty("response_schema").GetProperty("required")
            .EnumerateArray().Select(e => e.GetString()).Should().ContainSingle("matches");
    }

    [Fact]
    public async Task MatchAsync_ConfiguredSeedAndTemperature_AreSent()
    {
        _service = CreateService(_handler, new Dictionary<string, string?>
        {
            ["GEMINI_TEMPERATURE"] = "0.2",
            ["GEMINI_TOP_P"] = "0.9",
            ["GEMINI_SEED"] = "7"
        });
        _handler.Enqueue(OkGemini("""{"matches":[]}"""));

        await _service.MatchAsync(SampleRequest());

        var body = JsonDocument.Parse(_handler.RequestBodies.Single());
        var generationConfig = body.RootElement.GetProperty("generationConfig");

        generationConfig.GetProperty("temperature").GetDouble().Should().Be(0.2);
        generationConfig.GetProperty("top_p").GetDouble().Should().Be(0.9);
        generationConfig.GetProperty("seed").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task MatchAsync_DefaultPromptVersion_UsesV2AbsoluteScoringPrompt()
    {
        _handler.Enqueue(OkGemini("""{"matches":[]}"""));

        await _service.MatchAsync(SampleRequest());

        var body = _handler.RequestBodies.Single();
        body.Should().Contain("ABSOLUTE SCORING RULES");
        body.Should().Contain("NEVER compare, rank, curve, or normalize");
        body.Should().Contain("Score experience alignment BIDIRECTIONALLY");
        body.Should().Contain("CANDIDATES:");
    }

    [Fact]
    public async Task MatchAsync_ConfiguredPromptVersionV1_UsesV1Prompt()
    {
        _service = CreateService(_handler, new Dictionary<string, string?>
        {
            ["GEMINI_PROMPT_VERSION"] = "v1"
        });
        _handler.Enqueue(OkGemini("""{"matches":[]}"""));

        await _service.MatchAsync(SampleRequest());

        var body = _handler.RequestBodies.Single();
        body.Should().NotContain("ABSOLUTE SCORING RULES");
        body.Should().Contain("CRITICAL MATCHING CRITERIA TO EVALUATE");
    }

    [Fact]
    public async Task MatchAsync_SubsetCandidateRequest_OnlyIncludesThoseCandidates()
    {
        var fullRequest = SampleRequest();

        _handler.Enqueue(OkGemini("""{"matches":[]}"""));
        await _service.MatchAsync(fullRequest);
        var fullBody = _handler.RequestBodies.Single();

        var subsetRequest = new TalentMatchingRequest
        {
            Job = fullRequest.Job,
            Candidates = new List<TalentProfileForMatching> { fullRequest.Candidates[0] }
        };

        _handler.Enqueue(OkGemini("""{"matches":[]}"""));
        await _service.MatchAsync(subsetRequest);
        var subsetBody = _handler.RequestBodies.Last();

        fullBody.Should().Contain("talent-2");
        subsetBody.Should().NotContain("talent-2");
        subsetBody.Should().Contain("talent-1");
    }

    [Fact]
    public async Task MatchAsync_OutOfRangeScores_AreClampedAndRounded()
    {
        _handler.Enqueue(OkGemini("""{"matches":[{"talentUserId":"talent-1","score":150,"explanation":"too high"},{"talentUserId":"talent-2","score":-40,"explanation":"too low"},{"talentUserId":"talent-3","score":42.4567,"explanation":"rounded"}]}"""));

        var result = await _service.MatchAsync(SampleRequest());

        result.Matches[0].Score.Should().Be(100m);
        result.Matches[1].Score.Should().Be(0m);
        result.Matches[2].Score.Should().Be(42.46m);
    }

    [Fact]
    public async Task MatchAsync_FencedJsonResponse_ParsesAsDefense()
    {
        _handler.Enqueue(OkGemini("```json\n{\"matches\":[{\"talentUserId\":\"talent-1\",\"score\":77.5,\"explanation\":\"ok\"}]}\n```"));

        var result = await _service.MatchAsync(SampleRequest());

        result.Matches.Should().ContainSingle();
        result.Matches[0].TalentUserId.Should().Be("talent-1");
        result.Matches[0].Score.Should().Be(77.5m);
    }

    [Fact]
    public async Task MatchAsync_EmptyCandidates_ReturnsEmptyWithoutHttpCall()
    {
        var result = await _service.MatchAsync(new TalentMatchingRequest { Candidates = new List<TalentProfileForMatching>() });

        result.Matches.Should().BeEmpty();
        _handler.RequestBodies.Should().BeEmpty();
    }

    [Fact]
    public async Task MatchAsync_ApiFailure_Throws()
    {
        _handler.Enqueue(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("boom")
        });

        var act = () => _service.MatchAsync(SampleRequest());

        var exception = await act.Should().ThrowAsync<Exception>();
        exception.Which.Message.Should().Contain("InternalServerError");
    }

    private sealed class StubAiConfigurationProvider : IAiConfigurationProvider
    {
        private readonly string _modelName;
        private readonly string _apiKey;

        public StubAiConfigurationProvider(string modelName, string apiKey)
        {
            _modelName = modelName;
            _apiKey = apiKey;
        }

        public Task<(string ModelName, string ApiKey)> GetConfigAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult((_modelName, _apiKey));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();

        public List<string> RequestBodies { get; } = new();

        public void Enqueue(HttpResponseMessage response) => _responses.Enqueue(response);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));

            return _responses.Count > 0
                ? _responses.Dequeue()
                : throw new InvalidOperationException("No response was queued for this request.");
        }
    }
}
