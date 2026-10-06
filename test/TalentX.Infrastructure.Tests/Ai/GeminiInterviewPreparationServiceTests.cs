using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Infrastructure.Ai;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai;

public class GeminiInterviewPreparationServiceTests
{
    private const string ModelName = "gemini-test";
    private const string PromptVersion = "v-test";

    private readonly StubHttpMessageHandler _handler = new();
    private readonly GeminiInterviewPreparationService _service;

    public GeminiInterviewPreparationServiceTests()
    {
        _service = CreateService(_handler);
    }

    private static GeminiInterviewPreparationService CreateService(StubHttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GEMINI_BASE_URL"] = "https://unit.test",
                ["GEMINI_PROMPT_VERSION"] = PromptVersion,
                ["GEMINI_RETRY_BASE_DELAY_MS"] = "0"
            })
            .Build();

        var httpClient = new HttpClient(handler);
        return new GeminiInterviewPreparationService(
            httpClient,
            new StubAiConfigurationProvider(ModelName, "test-key"),
            configuration,
            NullLogger<GeminiInterviewPreparationService>.Instance);
    }

    private static AnswersEvaluationContext EvaluationContext()
    {
        return new AnswersEvaluationContext
        {
            JobTitle = "Backend Engineer",
            CompanyName = "Acme",
            TalentName = "Ada Lovelace",
            InterviewType = "OnCall",
            TotalQuestionCount = 2,
            AnsweredQuestions = new List<AnsweredQuestion>
            {
                new("Technical", 0, "What is a closure?",
                    "A closure captures its lexical scope."),
                new("Behavioral", 0, "Tell me about a conflict.",
                    "We disagreed on a design and I proposed a spike.")
            }
        };
    }

    private static string ValidReport(
        int overallScore = 78,
        int technicalScore = 80,
        int behavioralScore = 70)
    {
        return $$"""
        {
          "overallScore": {{overallScore}},
          "summary": "Solid answers with room for more structure.",
          "strengths": ["Concrete examples"],
          "areasToImprove": ["Add measurable outcomes"],
          "questionFeedback": [
            {"category":"Technical","questionIndex":0,"score":{{technicalScore}},"feedback":"Good.","suggestedAnswer":"Expand with an example."},
            {"category":"Behavioral","questionIndex":0,"score":{{behavioralScore}},"feedback":"Use STAR.","suggestedAnswer":""}
          ]
        }
        """;
    }

    private static HttpResponseMessage OkGemini(string innerJson)
    {
        var envelope = new
        {
            candidates = new[]
            {
                new { content = new { parts = new[] { new { text = innerJson } } } }
            },
            usageMetadata = new
            {
                promptTokenCount = 120,
                candidatesTokenCount = 60,
                totalTokenCount = 180
            }
        };

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(envelope), Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task EvaluateAnswersAsync_WithValidReport_ReturnsResultAndCallMetadata()
    {
        _handler.Enqueue(OkGemini(ValidReport()));

        var result = await _service.EvaluateAnswersAsync(EvaluationContext(), CancellationToken.None);

        result.OverallScore.Should().Be(78);
        result.Summary.Should().Be("Solid answers with room for more structure.");
        result.QuestionFeedback.Should().Contain("Technical");

        result.Metadata.Should().NotBeNull();
        result.Metadata!.ModelName.Should().Be(ModelName);
        result.Metadata.PromptVersion.Should().Be(PromptVersion);
        result.Metadata.InputTokenCount.Should().Be(120);
        result.Metadata.OutputTokenCount.Should().Be(60);
        result.Metadata.LatencyMs.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task EvaluateAnswersAsync_SendsEvaluationResponseSchema()
    {
        _handler.Enqueue(OkGemini(ValidReport()));

        await _service.EvaluateAnswersAsync(EvaluationContext(), CancellationToken.None);

        var body = _handler.RequestBodies.Should().ContainSingle().Subject;
        body.Should().Contain("response_schema");
        body.Should().Contain("questionFeedback");
        body.Should().Contain("overallScore");
        _handler.Requests[0].RequestUri!.AbsoluteUri.Should().Contain(
            $"/v1beta/models/{ModelName}:generateContent?key=test-key");
    }

    [Fact]
    public async Task EvaluateAnswersAsync_WhenReportIsFencedWithBackticks_ParsesIt()
    {
        _handler.Enqueue(OkGemini("```json\n" + ValidReport() + "\n```"));

        var result = await _service.EvaluateAnswersAsync(EvaluationContext(), CancellationToken.None);

        result.OverallScore.Should().Be(78);
    }

    [Fact]
    public async Task EvaluateAnswersAsync_WhenOverallScoreIsMissing_Throws()
    {
        _handler.Enqueue(OkGemini("""
        {
          "summary": "no score here",
          "strengths": [],
          "areasToImprove": [],
          "questionFeedback": [
            {"category":"Technical","questionIndex":0,"score":80,"feedback":"Good."},
            {"category":"Behavioral","questionIndex":0,"score":70,"feedback":"Use STAR."}
          ]
        }
        """));

        var act = () => _service.EvaluateAnswersAsync(EvaluationContext(), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*overallScore*");
    }

    [Fact]
    public async Task EvaluateAnswersAsync_WhenQuestionFeedbackIsMissing_Throws()
    {
        _handler.Enqueue(OkGemini("""
        {"overallScore": 75, "summary": "ok", "strengths": [], "areasToImprove": []}
        """));

        var act = () => _service.EvaluateAnswersAsync(EvaluationContext(), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*questionFeedback*");
    }

    [Fact]
    public async Task EvaluateAnswersAsync_WhenFeedbackEntryIsMalformed_Throws()
    {
        _handler.Enqueue(OkGemini("""
        {
          "overallScore": 75,
          "summary": "ok",
          "strengths": [],
          "areasToImprove": [],
          "questionFeedback": [
            {"category":"Technical","questionIndex":0,"score":"not-a-number","feedback":"Bad."},
            {"category":"Behavioral","questionIndex":0,"score":70,"feedback":"Use STAR."}
          ]
        }
        """));

        var act = () => _service.EvaluateAnswersAsync(EvaluationContext(), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*malformed*");
    }

    [Fact]
    public async Task EvaluateAnswersAsync_WhenFeedbackHasDuplicates_Throws()
    {
        _handler.Enqueue(OkGemini("""
        {
          "overallScore": 75,
          "summary": "ok",
          "strengths": [],
          "areasToImprove": [],
          "questionFeedback": [
            {"category":"Technical","questionIndex":0,"score":80,"feedback":"Good."},
            {"category":"Technical","questionIndex":0,"score":60,"feedback":"Also fine."},
            {"category":"Behavioral","questionIndex":0,"score":70,"feedback":"Use STAR."}
          ]
        }
        """));

        var act = () => _service.EvaluateAnswersAsync(EvaluationContext(), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*duplicate*");
    }

    [Fact]
    public async Task EvaluateAnswersAsync_WhenCoverageIsIncomplete_Throws()
    {
        _handler.Enqueue(OkGemini("""
        {
          "overallScore": 75,
          "summary": "ok",
          "strengths": [],
          "areasToImprove": [],
          "questionFeedback": [
            {"category":"Technical","questionIndex":0,"score":80,"feedback":"Good."}
          ]
        }
        """));

        var act = () => _service.EvaluateAnswersAsync(EvaluationContext(), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*does not cover*");
    }

    [Fact]
    public async Task EvaluateAnswersAsync_WhenScoresAreOutOfRange_ClampsThem()
    {
        _handler.Enqueue(OkGemini(ValidReport(overallScore: 150, technicalScore: -5)));

        var result = await _service.EvaluateAnswersAsync(EvaluationContext(), CancellationToken.None);

        result.OverallScore.Should().Be(100);
        result.QuestionFeedback.Should().Contain("\"Score\":0");
    }

    [Fact]
    public async Task EvaluateAnswersAsync_WhenGeminiReturnsEmptyContent_Throws()
    {
        _handler.Enqueue(OkGemini(""));
        _handler.Enqueue(OkGemini(""));
        _handler.Enqueue(OkGemini(""));

        var act = () => _service.EvaluateAnswersAsync(EvaluationContext(), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*empty content*");
        _handler.Requests.Should().HaveCount(3);
    }

    [Fact]
    public async Task EvaluateAnswersAsync_WhenGeminiReturns400_FailsWithoutRetry()
    {
        _handler.Enqueue(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("bad request")
        });

        var act = () => _service.EvaluateAnswersAsync(EvaluationContext(), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*BadRequest*");
        _handler.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task EvaluateAnswersAsync_WhenGeminiReturns500_RetriesThenSucceeds()
    {
        _handler.Enqueue(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("boom")
        });
        _handler.Enqueue(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("slow down")
        });
        _handler.Enqueue(OkGemini(ValidReport()));

        var result = await _service.EvaluateAnswersAsync(EvaluationContext(), CancellationToken.None);

        result.OverallScore.Should().Be(78);
        _handler.Requests.Should().HaveCount(3);
    }

    [Fact]
    public async Task EvaluateAnswersAsync_WhenGeminiKeepsFailing_ThrowsAfterMaxAttempts()
    {
        _handler.Enqueue(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("boom 1")
        });
        _handler.Enqueue(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("boom 2")
        });
        _handler.Enqueue(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("boom 3")
        });

        var act = () => _service.EvaluateAnswersAsync(EvaluationContext(), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*InternalServerError*");
        _handler.Requests.Should().HaveCount(3);
    }

    [Fact]
    public async Task GeneratePreparationAsync_WithValidResponse_ParsesResultAndSendsSchema()
    {
        _handler.Enqueue(OkGemini("""
        {
          "technicalQuestions": [{"question":"What is a closure?","category":"Technical","tip":"Scope"}],
          "behavioralQuestions": [],
          "experienceBasedQuestions": [],
          "roleSpecificQuestions": [],
          "keyTopicsToPrepare": ["Idempotency"],
          "suggestedRevisionAreas": ["SQL tuning"]
        }
        """));

        var result = await _service.GeneratePreparationAsync(
            new InterviewPreparationContext(), CancellationToken.None);

        result.TechnicalQuestions.Should().Contain("What is a closure?");
        result.Metadata.Should().NotBeNull();
        result.Metadata!.ModelName.Should().Be(ModelName);

        var body = _handler.RequestBodies.Should().ContainSingle().Subject;
        body.Should().Contain("response_schema");
        body.Should().Contain("technicalQuestions");
    }

    [Fact]
    public async Task GenerateAdditionalQuestionsAsync_WithValidResponse_ParsesResult()
    {
        _handler.Enqueue(OkGemini("""
        {"additionalQuestions": [{"question":"How do you shard a queue?","category":"Role-Specific","tip":"Think partitions"}]}
        """));

        var result = await _service.GenerateAdditionalQuestionsAsync(
            new AdditionalQuestionsContext(), CancellationToken.None);

        result.AdditionalQuestions.Should().Contain("How do you shard a queue?");
        result.Metadata.Should().NotBeNull();
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

        public List<HttpRequestMessage> Requests { get; } = new();
        public List<string> RequestBodies { get; } = new();

        public void Enqueue(HttpResponseMessage response) => _responses.Enqueue(response);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            RequestBodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));

            return _responses.Count > 0
                ? _responses.Dequeue()
                : throw new InvalidOperationException("No response was queued for this request.");
        }
    }
}
