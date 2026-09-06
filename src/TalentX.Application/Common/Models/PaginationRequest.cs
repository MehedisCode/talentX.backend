namespace TalentX.Application.Common.Models;

// Page/pageSize clamping lives here once, instead of being re-implemented per handler.
public static class PaginationRequest
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var normalizedPage = page < DefaultPageNumber ? DefaultPageNumber : page;
        var normalizedPageSize = pageSize is < 1 or > MaxPageSize ? DefaultPageSize : pageSize;
        return (normalizedPage, normalizedPageSize);
    }
}
