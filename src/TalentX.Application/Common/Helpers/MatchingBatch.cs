using Microsoft.Extensions.Configuration;

namespace TalentX.Application.Common.Helpers;

public static class MatchingBatch
{
    public const int DefaultSize = 50;

    public static int ResolveSize(IConfiguration? configuration)
    {
        var raw = configuration?["GEMINI_MATCH_BATCH_SIZE"] ?? configuration?["Gemini:MatchBatchSize"];
        return int.TryParse(raw, out var value) && value > 0 ? value : DefaultSize;
    }
}
