using Microsoft.EntityFrameworkCore;

namespace Portfolio.Web.Infrastructure;

/// <summary>Consistent pagination metadata shared by public and admin list pages.</summary>
public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 9;
    public int TotalCount { get; init; }

    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static async Task<PagedList<T>> CreateAsync(
        IQueryable<T> source, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await source.CountAsync(ct);
        var items = await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedList<T> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }
}

public class ListQuery
{
    public string? Q { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public bool IsPublishedFilter => Status == "published";
    public bool IsDraftFilter => Status == "draft";
    public bool IsArchivedFilter => Status == "archived";
}
