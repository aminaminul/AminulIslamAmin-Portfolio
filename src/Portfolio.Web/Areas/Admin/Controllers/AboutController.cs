using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class AboutController : AdminControllerBase
{
    private readonly IMarkdownService _markdown;

    public AboutController(Data.AppDbContext db, IMediaService media, IMarkdownService markdown) : base(db, media)
    {
        _markdown = markdown;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = await Db.AboutSections.AsNoTracking()
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await LoadImageChoicesAsync(ct, "Image");
        var next = await Db.AboutSections.CountAsync(ct) + 1;
        return View("Form", new AboutFormModel { DisplayOrder = next, Status = ContentStatus.Draft, IsVisible = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AboutFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadImageChoicesAsync(ct, "Image");
            return View("Form", form);
        }

        try
        {
            form.ImageMediaId = await KeepOrReplaceAsync(form.ImageFile, form.ImageMediaId, MediaKind.Image, null, ct);
        }
        catch (MediaValidationException)
        {
            await LoadImageChoicesAsync(ct, "Image");
            return View("Form", form);
        }

        var entity = new AboutSection();
        form.ApplyTo(entity);
        SetPublishedAt(entity, form.Status);
        Db.AboutSections.Add(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("About section created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await Db.AboutSections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        await LoadImageChoicesAsync(ct, "Image");
        return View("Form", AboutFormModel.From(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AboutFormModel form, CancellationToken ct)
    {
        if (id != form.Id) return BadRequest();
        var entity = await Db.AboutSections.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadImageChoicesAsync(ct, "Image");
            return View("Form", form);
        }

        try
        {
            form.ImageMediaId = await KeepOrReplaceAsync(form.ImageFile, form.ImageMediaId, MediaKind.Image, null, ct);
        }
        catch (MediaValidationException)
        {
            await LoadImageChoicesAsync(ct, "Image");
            return View("Form", form);
        }

        form.ApplyTo(entity);
        SetPublishedAt(entity, form.Status);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("About section updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.AboutSections.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        Db.AboutSections.Remove(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("About section deleted.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction, CancellationToken ct)
    {
        var items = await Db.AboutSections.ToListAsync(ct);
        await MoveAsync(items, id, direction, x => x.Id, x => x.DisplayOrder, (x, o) => x.DisplayOrder = o, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int id, CancellationToken ct)
    {
        var entity = await Db.AboutSections.AsNoTracking().Include(x => x.Image).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        ViewData["BodyHtml"] = _markdown.ToHtml(ContentLocalization.IsBangla && entity.BodyBn is not null ? entity.BodyBn : entity.Body);
        return View(entity);
    }
}
