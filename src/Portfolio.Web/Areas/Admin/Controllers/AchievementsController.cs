using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class AchievementsController : AdminControllerBase
{
    public AchievementsController(Data.AppDbContext db, IMediaService media) : base(db, media) { }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int page = 1, CancellationToken ct = default)
    {
        var query = Db.Achievements.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Title.Contains(q.Trim()) || (x.IssuingOrganization != null && x.IssuingOrganization.Contains(q.Trim())));

        var items = await PagedList<Achievement>.CreateAsync(
            query.OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.AchievementDate), page, AdminPageSize, ct);

        return View(new AdminListViewModel<Achievement>
        {
            Items = items.Items,
            Q = q,
            Pager = Pager(items.Page, items.TotalPages, p => Url.Action(nameof(Index), new { q, page = p })!)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await LoadImageChoicesAsync(ct, "Image");
        var next = await Db.Achievements.CountAsync(ct) + 1;
        return View("Form", new AchievementFormModel { DisplayOrder = next, IsVisible = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AchievementFormModel form, CancellationToken ct)
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

        var entity = new Achievement();
        form.ApplyTo(entity);
        Db.Achievements.Add(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Achievement created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await Db.Achievements.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        await LoadImageChoicesAsync(ct, "Image");
        return View("Form", AchievementFormModel.From(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AchievementFormModel form, CancellationToken ct)
    {
        if (id != form.Id) return BadRequest();
        var entity = await Db.Achievements.FirstOrDefaultAsync(x => x.Id == id, ct);
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
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Achievement updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.Achievements.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        Db.Achievements.Remove(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Achievement deleted.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVisible(int id, CancellationToken ct)
    {
        var entity = await Db.Achievements.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsVisible = !entity.IsVisible;
        await Db.SaveChangesAsync(ct);
        NotifySuccess(entity.IsVisible ? "Achievement visible." : "Achievement hidden.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction, CancellationToken ct)
    {
        var items = await Db.Achievements.ToListAsync(ct);
        await MoveAsync(items, id, direction, x => x.Id, x => x.DisplayOrder, (x, o) => x.DisplayOrder = o, ct);
        return RedirectToAction(nameof(Index));
    }
}
