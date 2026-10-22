using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class SocialLinksController : AdminControllerBase
{
    public SocialLinksController(Data.AppDbContext db, IMediaService media) : base(db, media) { }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = await Db.SocialLinks.AsNoTracking()
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        ViewData["Icons"] = IconCatalog.SocialNames;
        return View(items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Icons"] = IconCatalog.SocialNames;
        var next = Db.SocialLinks.Count() + 1;
        return View("Form", new SocialLinkFormModel { DisplayOrder = next, IsVisible = true, ShowInHero = true, ShowInFooter = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SocialLinkFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Icons"] = IconCatalog.SocialNames;
            return View("Form", form);
        }
        Db.SocialLinks.Add(new SocialLink
        {
            Platform = form.Platform.Trim(),
            Url = form.Url.Trim(),
            IconName = string.IsNullOrWhiteSpace(form.IconName) ? null : form.IconName.Trim(),
            DisplayOrder = form.DisplayOrder,
            IsVisible = form.IsVisible,
            ShowInHero = form.ShowInHero,
            ShowInFooter = form.ShowInFooter
        });
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Social link added.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await Db.SocialLinks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        ViewData["Icons"] = IconCatalog.SocialNames;
        return View("Form", SocialLinkFormModel.From(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SocialLinkFormModel form, CancellationToken ct)
    {
        if (id != form.Id) return BadRequest();
        var entity = await Db.SocialLinks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (!ModelState.IsValid)
        {
            ViewData["Icons"] = IconCatalog.SocialNames;
            return View("Form", form);
        }

        entity.Platform = form.Platform.Trim();
        entity.Url = form.Url.Trim();
        entity.IconName = string.IsNullOrWhiteSpace(form.IconName) ? null : form.IconName.Trim();
        entity.DisplayOrder = form.DisplayOrder;
        entity.IsVisible = form.IsVisible;
        entity.ShowInHero = form.ShowInHero;
        entity.ShowInFooter = form.ShowInFooter;
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Social link updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.SocialLinks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        Db.SocialLinks.Remove(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Social link deleted.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVisible(int id, CancellationToken ct)
    {
        var entity = await Db.SocialLinks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsVisible = !entity.IsVisible;
        await Db.SaveChangesAsync(ct);
        NotifySuccess(entity.IsVisible ? "Social link shown." : "Social link hidden.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction, CancellationToken ct)
    {
        var items = await Db.SocialLinks.ToListAsync(ct);
        await MoveAsync(items, id, direction, x => x.Id, x => x.DisplayOrder, (x, o) => x.DisplayOrder = o, ct);
        return RedirectToAction(nameof(Index));
    }
}
