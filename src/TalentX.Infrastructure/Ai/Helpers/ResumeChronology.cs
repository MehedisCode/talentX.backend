using TalentX.Application.Features.Talents.Helpers;

namespace TalentX.Infrastructure.Ai.Helpers;

/// <summary>
/// Adapts chronological sorting for AI resume parsing, delegating core chronology
/// logic to <see cref="ChronologicalOrderingHelper"/>.
/// </summary>
public static class ResumeChronology
{
    public const int CurrentKey = ChronologicalOrderingHelper.CurrentKey;

    public static int? ToSortKey(string? value) =>
        ChronologicalOrderingHelper.ToSortKey(value);

    public static List<T> SortNewestFirst<T>(IEnumerable<T> items, Func<T, int?> selectKey) =>
        ChronologicalOrderingHelper.SortNewestFirst(items, selectKey);

    public static List<T> SortWorkExperiences<T>(
        IEnumerable<T> items,
        Func<T, (bool IsCurrent, string? StartDate, string? EndDate)> selector) =>
        ChronologicalOrderingHelper.SortWorkExperiences(items, selector);

    public static List<T> SortEducations<T>(
        IEnumerable<T> items,
        Func<T, (string? StartDate, string? EndDate)> selector) =>
        ChronologicalOrderingHelper.SortEducations(items, selector);
}
