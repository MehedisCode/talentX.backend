
using Microsoft.EntityFrameworkCore;

namespace TalentX.Infrastructure.Persistence.Repositories;

internal static class TimeBucketCounter
{
    // Counts timestamps per UTC hour or per UTC day, keyed by the bucket's start. Buckets with no rows are absent.
    public static async Task<IReadOnlyDictionary<DateTime, int>> CountAsync(
        IQueryable<DateTime> timestamps,
        bool groupByHour,
        CancellationToken cancellationToken)
    {
        if (groupByHour)
        {
            var hourly = await timestamps
                .GroupBy(t => new { t.Date, t.Hour })
                .Select(g => new { g.Key.Date, g.Key.Hour, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return hourly.ToDictionary(
                row => DateTime.SpecifyKind(row.Date.AddHours(row.Hour), DateTimeKind.Utc),
                row => row.Count);
        }

        var daily = await timestamps
            .GroupBy(t => t.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return daily.ToDictionary(
            row => DateTime.SpecifyKind(row.Date, DateTimeKind.Utc),
            row => row.Count);
    }
}
