using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentX.Infrastructure.Ai;
using TalentX.Tools.InterviewEvaluationHarness;

// ---------------------------------------------------------------------------
// CLI
// ---------------------------------------------------------------------------
var casesPath = (string?)null;
int? limit = null;
var dryRun = false;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--cases" when i + 1 < args.Length:
            casesPath = args[++i];
            break;
        case "--limit" when i + 1 < args.Length:
            if (!int.TryParse(args[++i], out var parsedLimit) || parsedLimit <= 0)
            {
                Console.Error.WriteLine("--limit must be a positive integer.");
                return 2;
            }

            limit = parsedLimit;
            break;
        case "--dry-run":
            dryRun = true;
            break;
        case "--help" or "-h":
            PrintUsage();
            return 0;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            PrintUsage();
            return 2;
    }
}

// ---------------------------------------------------------------------------
// Configuration — mirrors TalentX.API/Program.cs so the harness grades with
// the exact runtime settings (env keys beat appsettings, .env feeds env vars).
// ---------------------------------------------------------------------------
LoadDotEnv(FindUp(AppContext.BaseDirectory, ".env") ?? FindUp(Directory.GetCurrentDirectory(), ".env"));

var configurationBuilder = new ConfigurationBuilder();
var appsettingsPath =
    FindUp(Directory.GetCurrentDirectory(), Path.Combine("src", "TalentX.API", "appsettings.json"))
    ?? FindUp(AppContext.BaseDirectory, Path.Combine("src", "TalentX.API", "appsettings.json"));
if (appsettingsPath is not null)
{
    configurationBuilder.AddJsonFile(appsettingsPath, optional: false);
}

configurationBuilder.AddEnvironmentVariables();
var configuration = configurationBuilder.Build();

// ---------------------------------------------------------------------------
// Golden set
// ---------------------------------------------------------------------------
var resolvedCasesPath = casesPath ?? Path.Combine(AppContext.BaseDirectory, "golden", "answers.json");
if (!File.Exists(resolvedCasesPath))
{
    Console.Error.WriteLine($"Golden set not found: {resolvedCasesPath}");
    return 2;
}

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var cases = JsonSerializer.Deserialize<List<GoldenCase>>(
    await File.ReadAllTextAsync(resolvedCasesPath), jsonOptions);

if (cases is null || cases.Count == 0)
{
    Console.Error.WriteLine($"Golden set is empty: {resolvedCasesPath}");
    return 2;
}

if (limit is not null)
{
    cases = cases.Take(limit.Value).ToList();
}

if (dryRun)
{
    foreach (var c in cases)
    {
        Console.WriteLine(
            $"{c.Id,-34} expected {c.ExpectedScore,3} ({Band(c.ExpectedScore),-14}) " +
            $"{c.Questions.Count} answered · context OK");
    }

    Console.WriteLine($"\nDry run: {cases.Count} case(s) loaded from {resolvedCasesPath}. No Gemini calls made.");
    return 0;
}

if (string.IsNullOrWhiteSpace(configuration["GEMINI_API_KEY"]))
{
    Console.Error.WriteLine(
        "GEMINI_API_KEY is not set (export it, or run from inside the repo so .env is found). " +
        "Use --dry-run to validate the golden set without calling Gemini.");
    return 2;
}

using var loggerFactory = LoggerFactory.Create(logging => logging
    .AddSimpleConsole(options => options.SingleLine = true)
    .SetMinimumLevel(LogLevel.Warning));
var service = new GeminiInterviewPreparationService(
    new HttpClient(),
    configuration,
    loggerFactory.CreateLogger<GeminiInterviewPreparationService>());

// ---------------------------------------------------------------------------
// Run
// ---------------------------------------------------------------------------
Console.WriteLine($"Evaluating {cases.Count} golden case(s) from {resolvedCasesPath} …\n");
Console.WriteLine($"{"ID",-34} {"EXP",3} {"ACT",3} {"Δ",4} {"EXPECTED BAND",-14} {"PREDICTED BAND",-14} COV");

var rows = new List<ResultRow>();
var attemptedAnswered = 0;
var covered = 0;
long latencyTotalMs = 0;
long inputTokens = 0;
long outputTokens = 0;
string modelName = string.Empty;
string promptVersion = string.Empty;

foreach (var c in cases)
{
    attemptedAnswered += c.Questions.Count;

    try
    {
        var result = await service.EvaluateAnswersAsync(c.ToContext());
        var feedbackCount = CountFeedback(result.QuestionFeedback);
        covered += Math.Min(feedbackCount, c.Questions.Count);

        if (result.Metadata is { } meta)
        {
            latencyTotalMs += meta.LatencyMs;
            inputTokens += meta.InputTokenCount;
            outputTokens += meta.OutputTokenCount;
            modelName = meta.ModelName;
            promptVersion = meta.PromptVersion;
        }

        var predicted = result.OverallScore;
        var delta = predicted - c.ExpectedScore;
        rows.Add(new ResultRow(c.Id, c.ExpectedScore, predicted, feedbackCount, c.Questions.Count, null));

        Console.WriteLine(
            $"{c.Id,-34} {c.ExpectedScore,3} {predicted,3} {delta,5:+#0;-#0;0} " +
            $"{Band(c.ExpectedScore),-14} {Band(predicted),-14} {feedbackCount}/{c.Questions.Count}");
    }
    catch (Exception ex)
    {
        rows.Add(new ResultRow(c.Id, c.ExpectedScore, null, 0, c.Questions.Count, ex.Message));
        Console.WriteLine(
            $"{c.Id,-34} {c.ExpectedScore,3} {"ERR",3} {"",4} {Band(c.ExpectedScore),-14} {"-",14} 0/{c.Questions.Count}");
        Console.Error.WriteLine($"    ↳ {ex.Message}");
    }
}

// ---------------------------------------------------------------------------
// Metrics
// ---------------------------------------------------------------------------
var successful = rows.Where(r => r.Predicted is not null).ToList();
var failed = rows.Count - successful.Count;

Console.WriteLine();
Console.WriteLine($"Cases:        {rows.Count} evaluated · {successful.Count} scored · {failed} failed");

if (successful.Count > 0)
{
    var mae = successful.Average(r => Math.Abs(r.Predicted!.Value - r.Expected));
    var maxError = successful.Max(r => Math.Abs(r.Predicted!.Value - r.Expected));
    var bandMatches = successful.Count(r => Band(r.Predicted!.Value) == Band(r.Expected));

    Console.WriteLine($"MAE:          {mae:F1} (max |error| {maxError})");
    Console.WriteLine(
        $"Band agree:   {bandMatches}/{successful.Count} " +
        $"({bandMatches * 100.0 / successful.Count:F1}%) · bands ≥85 Strong, ≥70 Good, ≥50 Moderate");
    Console.WriteLine(
        $"Coverage:     {covered}/{attemptedAnswered} answered questions received feedback " +
        $"({(attemptedAnswered == 0 ? 100.0 : covered * 100.0 / attemptedAnswered):F1}%)");
    Console.WriteLine(
        $"Call stats:   model {modelName} · prompt {promptVersion} · " +
        $"avg latency {latencyTotalMs / successful.Count}ms · " +
        $"tokens in/out {inputTokens}/{outputTokens}");
}

return failed == 0 ? 0 : 1;

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
static string Band(int score) => score switch
{
    >= 85 => "Strong",
    >= 70 => "Good",
    >= 50 => "Moderate",
    _ => "Needs practice",
};

static int CountFeedback(string json)
{
    if (string.IsNullOrWhiteSpace(json)) return 0;
    using var doc = JsonDocument.Parse(json);
    return doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.GetArrayLength() : 0;
}

static string? FindUp(string? start, string relativePath, int maxDepth = 8)
{
    var dir = start;
    for (var i = 0; i < maxDepth && !string.IsNullOrEmpty(dir); i++)
    {
        var candidate = Path.Combine(dir, relativePath);
        if (File.Exists(candidate)) return candidate;
        dir = Path.GetDirectoryName(dir);
    }

    return null;
}

// Same .env semantics as TalentX.API/Program.cs: plain KEY=VALUE lines written
// into the process environment (existing vars are overwritten, as in the API).
static void LoadDotEnv(string? envFile)
{
    if (envFile is null || !File.Exists(envFile)) return;

    foreach (var line in File.ReadAllLines(envFile))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#')) continue;
        var idx = trimmed.IndexOf('=');
        if (idx <= 0) continue;
        var key = trimmed[..idx].Trim();
        var value = trimmed[(idx + 1)..].Trim().Trim('"').Trim('\'');
        Environment.SetEnvironmentVariable(key, value);
    }
}

static void PrintUsage()
{
    Console.WriteLine("""
        Interview evaluation harness — grades golden practice answers through
        EvaluateAnswersAsync and reports MAE / band agreement / coverage.

        Usage:
          dotnet run --project tools/InterviewEvaluationHarness -- [options]

        Options:
          --cases <path>   Golden set JSON (default: golden/answers.json next to the binary)
          --limit <n>      Only run the first n cases (smoke run)
          --dry-run        Load and validate the golden set without calling Gemini
          --help, -h       Show this help

        Exit codes: 0 = all cases scored, 1 = some cases failed, 2 = usage/config error.
        """);
}

internal sealed record ResultRow(
    string Id,
    int Expected,
    int? Predicted,
    int FeedbackCount,
    int Answered,
    string? Error);
