using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class CertificationsController : AdminControllerBase
{
    public CertificationsController(Data.AppDbContext db, IMediaService media) : base(db, media) { }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? status, int page = 1, CancellationToken ct = default)
    {
        var query = Db.Certifications.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Title.Contains(q.Trim()) || x.Issuer.Contains(q.Trim()));
        if (status == "published") query = query.Where(x => x.Status == ContentStatus.Published);
        else if (status == "draft") query = query.Where(x => x.Status == ContentStatus.Draft);

        var items = await PagedList<Certification>.CreateAsync(
            query.OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.IssueDate), page, AdminPageSize, ct);

        return View(new AdminListViewModel<Certification>
        {
            Items = items.Items,
            Q = q,
            Status = status,
            Pager = Pager(items.Page, items.TotalPages, p => Url.Action(nameof(Index), new { q, status, page = p })!)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await LoadImageChoicesAsync(ct, "Image");
        var next = await Db.Certifications.CountAsync(ct) + 1;
        return View("Form", new CertificationFormModel { DisplayOrder = next, Status = ContentStatus.Draft });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CertificationFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadImageChoicesAsync(ct, "Image");
            return View("Form", form);
        }
        try
        {
            form.ImageMediaId = await KeepOrReplaceAsync(form.ImageFile, form.ImageMediaId, MediaKind.Image, form.Title, ct);
        }
        catch (MediaValidationException)
        {
            await LoadImageChoicesAsync(ct, "Image");
            return View("Form", form);
        }

        var entity = new Certification();
        form.ApplyTo(entity);
        SetPublishedAt(entity, form.Status);
        Db.Certifications.Add(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Certification created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await Db.Certifications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        await LoadImageChoicesAsync(ct, "Image");
        return View("Form", CertificationFormModel.From(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CertificationFormModel form, CancellationToken ct)
    {
        if (id != form.Id) return BadRequest();
        var entity = await Db.Certifications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (!ModelState.IsValid)
        {
            await LoadImageChoicesAsync(ct, "Image");
            return View("Form", form);
        }
        try
        {
            form.ImageMediaId = await KeepOrReplaceAsync(form.ImageFile, form.ImageMediaId, MediaKind.Image, form.Title, ct);
        }
        catch (MediaValidationException)
        {
            await LoadImageChoicesAsync(ct, "Image");
            return View("Form", form);
        }

        form.ApplyTo(entity);
        SetPublishedAt(entity, form.Status);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Certification updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.Certifications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        Db.Certifications.Remove(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Certification deleted.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, CancellationToken ct) => await SetStatusAsync(id, ContentStatus.Published, ct);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(int id, CancellationToken ct) => await SetStatusAsync(id, ContentStatus.Draft, ct);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction, CancellationToken ct)
    {
        var items = await Db.Certifications.ToListAsync(ct);
        await MoveAsync(items, id, direction, x => x.Id, x => x.DisplayOrder, (x, o) => x.DisplayOrder = o, ct);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SetStatusAsync(int id, ContentStatus status, CancellationToken ct)
    {
        var entity = await Db.Certifications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        SetPublishedAt(entity, status);
        await Db.SaveChangesAsync(ct);
        NotifySuccess(status == ContentStatus.Published ? "Certification published." : "Certification moved to draft.");
        return RedirectToAction(nameof(Index));
    }
}
