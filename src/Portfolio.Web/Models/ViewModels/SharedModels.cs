using Portfolio.Web.Models.Entities;

namespace Portfolio.Web.Models.ViewModels;

public record MediaFieldModel(
    string Name,
    string Label,
    int? SelectedId,
    IEnumerable<MediaFile> Choices,
    string? Hint = null,
    bool Document = false)
{
    public MediaFile? Selected => Choices.FirstOrDefault(c => c.Id == SelectedId);
}

public record PagerModel(int Page, int TotalPages, Func<int, string> UrlFor);

public class AdminListViewModel<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public string? Q { get; init; }
    public string? Status { get; init; }
    public string? Filter { get; init; }
    public PagerModel? Pager { get; init; }
    public bool HasFilters => !string.IsNullOrEmpty(Q) || !string.IsNullOrEmpty(Status) || !string.IsNullOrEmpty(Filter);
}

public record StatusBadgeModel(ContentStatus Status, DateTime? PublishedAt = null);

public record EmptyStateModel(string Title, string? Message = null, string? Icon = "folder", string? ActionUrl = null, string? ActionLabel = null);
