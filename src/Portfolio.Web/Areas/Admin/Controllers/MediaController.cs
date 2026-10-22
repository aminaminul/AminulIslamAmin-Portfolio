using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class MediaController : AdminControllerBase
{
    public MediaController(Data.AppDbContext db, IMediaService media) : base(db, media) { }

    [HttpGet]
    public async Task<IActionResult> Index(string? kind, int page = 1, CancellationToken ct = default)
    {
        var query = Db.MediaFiles.AsNoTracking().AsQueryable();
        if (kind == "images") query = query.Where(m => m.Kind == MediaKind.Image);
        else if (kind == "documents") query = query.Where(m => m.Kind == MediaKind.Document);

        var items = await PagedList<MediaFile>.CreateAsync(
            query.OrderByDescending(m => m.CreatedAt), page, 24, ct);

        ViewData["Kind"] = kind;
        return View(new AdminListViewModel<MediaFile>
        {
            Items = items.Items,
            Pager = Pager(items.Page, items.TotalPages, p => Url.Action(nameof(Index), new { kind, page = p })!)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Upload(MediaUploadViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid || model.File is null)
        {
            NotifyError("Choose a file to upload.");
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var media = await Media.SaveAsync(model.File, model.Kind, model.AltText, CurrentUserName, ct);
            NotifySuccess($"Uploaded \"{media.OriginalName}\". It is available in every image/document picker.");
        }
        catch (MediaValidationException ex)
        {
            NotifyError(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { kind = model.Kind == MediaKind.Image ? "images" : "documents" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAlt(int id, string? altText, CancellationToken ct)
    {
        var entity = await Db.MediaFiles.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity is null) return NotFound();
        entity.AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim()[..Math.Min(altText.Trim().Length, 250)];
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Alt text updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            var deleted = await Media.DeleteAsync(id, ct: ct);
            if (!deleted) return NotFound();
            NotifySuccess("File deleted.");
        }
        catch (MediaInUseException ex)
        {
            NotifyError(ex.Message + " Used by: " + string.Join(", ", ex.Usages.Select(u => u.Description).Distinct()));
        }
        return RedirectToAction(nameof(Index));
    }
}
