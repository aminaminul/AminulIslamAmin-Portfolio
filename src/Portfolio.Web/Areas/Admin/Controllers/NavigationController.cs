using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class NavigationController : AdminControllerBase
{
    public NavigationController(Data.AppDbContext db, IMediaService media) : base(db, media) { }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = await Db.NavigationItems.AsNoTracking()
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var next = await Db.NavigationItems.CountAsync(ct) + 1;
        return View("Form", new NavigationFormModel { DisplayOrder = next, IsVisible = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NavigationFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("Form", form);
        Db.NavigationItems.Add(new NavigationItem
        {
            Label = form.Label.Trim(),
            LabelBn = Clean(form.LabelBn),
            Url = form.Url.Trim(),
            DisplayOrder = form.DisplayOrder,
            IsVisible = form.IsVisible,
            OpenInNewTab = form.OpenInNewTab,
            ShowInFooter = form.ShowInFooter,
            SectionKey = Clean(form.SectionKey)
        });
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Navigation link created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await Db.NavigationItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        return View("Form", NavigationFormModel.From(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NavigationFormModel form, CancellationToken ct)
    {
        if (id != form.Id) return BadRequest();
        var entity = await Db.NavigationItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (!ModelState.IsValid) return View("Form", form);

        entity.Label = form.Label.Trim();
        entity.LabelBn = Clean(form.LabelBn);
        entity.Url = form.Url.Trim();
        entity.DisplayOrder = form.DisplayOrder;
        entity.IsVisible = form.IsVisible;
        entity.OpenInNewTab = form.OpenInNewTab;
        entity.ShowInFooter = form.ShowInFooter;
        entity.SectionKey = Clean(form.SectionKey);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Navigation link updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.NavigationItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        Db.NavigationItems.Remove(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Navigation link deleted.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVisible(int id, CancellationToken ct)
    {
        var entity = await Db.NavigationItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsVisible = !entity.IsVisible;
        await Db.SaveChangesAsync(ct);
        NotifySuccess(entity.IsVisible ? "Link shown." : "Link hidden.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction, CancellationToken ct)
    {
        var items = await Db.NavigationItems.ToListAsync(ct);
        await MoveAsync(items, id, direction, x => x.Id, x => x.DisplayOrder, (x, o) => x.DisplayOrder = o, ct);
        return RedirectToAction(nameof(Index));
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
