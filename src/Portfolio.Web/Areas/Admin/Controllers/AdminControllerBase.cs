using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdministratorRole)]
public abstract class AdminControllerBase : Controller
{
    protected const int AdminPageSize = 12;

    protected AdminControllerBase(AppDbContext db, IMediaService media)
    {
        Db = db;
        Media = media;
    }

    protected AppDbContext Db { get; }
    protected IMediaService Media { get; }
    protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    protected string? CurrentUserName => User.Identity?.Name;

    protected void NotifySuccess(string message) => TempData["Success"] = message;
    protected void NotifyError(string message) => TempData["Error"] = message;

    /// <summary>Keeps the current media id unless a new file is uploaded.</summary>
    protected async Task<int?> KeepOrReplaceAsync(IFormFile? upload, int? currentId, MediaKind kind,
        string? altText, CancellationToken ct)
    {
        if (upload is null || upload.Length == 0) return currentId;
        try
        {
            var saved = await Media.SaveAsync(upload, kind, altText, CurrentUserName, ct);
            return saved.Id;
        }
        catch (MediaValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            throw;
        }
    }

    protected async Task LoadChoicesAsync(string fieldName, MediaKind kind, CancellationToken ct)
    {
        ViewData[$"{fieldName}Choices"] = await Media.ListAsync(kind, ct);
    }

    protected async Task LoadImageChoicesAsync(CancellationToken ct, params string[] fields)
    {
        var images = await Media.ListAsync(MediaKind.Image, ct);
        foreach (var field in fields) ViewData[$"{field}Choices"] = images;
    }

    protected MediaFieldModel MediaField(string name, string label, int? selectedId, string? hint = null, bool document = false) =>
        new(name, label, selectedId, ViewData[$"{name}Choices"] as IEnumerable<MediaFile> ?? [], hint, document);

    /// <summary>Swaps an item with its neighbour and rewrites sequential DisplayOrder values.</summary>
    protected async Task<bool> MoveAsync<T>(IReadOnlyList<T> items, int id, int direction,
        Func<T, int> idSelector, Func<T, int> orderSelector, Action<T, int> orderSetter,
        CancellationToken ct) where T : class
    {
        var ordered = items.OrderBy(orderSelector).ThenBy(idSelector).ToList();
        var index = ordered.FindIndex(x => idSelector(x) == id);
        if (index < 0) return false;

        var target = index + direction;
        if (target < 0 || target >= ordered.Count) return true;

        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var i = 0; i < ordered.Count; i++) orderSetter(ordered[i], i + 1);
        await Db.SaveChangesAsync(ct);
        return true;
    }

    protected static PagerModel Pager(int page, int totalPages, Func<int, string> urlFor) => new(page, totalPages, urlFor);

    protected static void SetPublishedAt(PublishableEntity entity, ContentStatus newStatus)
    {
        if (newStatus == ContentStatus.Published && entity.PublishedAt is null)
            entity.PublishedAt = DateTime.UtcNow;
        else if (newStatus != ContentStatus.Published)
            entity.PublishedAt = null;
        entity.Status = newStatus;
    }

    protected async Task<List<Research>> PublishedResearchChoicesAsync(int? excludeId, CancellationToken ct) =>
        await Db.Research.AsNoTracking()
            .Where(r => r.Status == ContentStatus.Published && (excludeId == null || r.Id != excludeId))
            .OrderByDescending(r => r.PublicationDate ?? r.CreatedAt)
            .Take(200)
            .ToListAsync(ct);
}
