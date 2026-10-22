using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class ServicesController : AdminControllerBase
{
    public ServicesController(Data.AppDbContext db, IMediaService media) : base(db, media) { }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int page = 1, CancellationToken ct = default)
    {
        var query = Db.Services.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Title.Contains(q.Trim()));

        var items = await PagedList<Service>.CreateAsync(
            query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id), page, AdminPageSize, ct);

        ViewData["Icons"] = IconCatalog.Names;
        return View(new AdminListViewModel<Service>
        {
            Items = items.Items,
            Q = q,
            Pager = Pager(items.Page, items.TotalPages, p => Url.Action(nameof(Index), new { q, page = p })!)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewData["Icons"] = IconCatalog.Names;
        var next = await Db.Services.CountAsync(ct) + 1;
        return View("Form", new ServiceFormModel { DisplayOrder = next, IsVisible = true, Currency = "USD" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Icons"] = IconCatalog.Names;
            return View("Form", form);
        }
        var entity = new Service();
        form.ApplyTo(entity);
        Db.Services.Add(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Service created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await Db.Services.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        ViewData["Icons"] = IconCatalog.Names;
        return View("Form", ServiceFormModel.From(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceFormModel form, CancellationToken ct)
    {
        if (id != form.Id) return BadRequest();
        var entity = await Db.Services.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (!ModelState.IsValid)
        {
            ViewData["Icons"] = IconCatalog.Names;
            return View("Form", form);
        }

        form.ApplyTo(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Service updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.Services.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        Db.Services.Remove(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Service deleted.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVisible(int id, CancellationToken ct)
    {
        var entity = await Db.Services.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsVisible = !entity.IsVisible;
        await Db.SaveChangesAsync(ct);
        NotifySuccess(entity.IsVisible ? "Service visible." : "Service hidden.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction, CancellationToken ct)
    {
        var items = await Db.Services.ToListAsync(ct);
        await MoveAsync(items, id, direction, x => x.Id, x => x.DisplayOrder, (x, o) => x.DisplayOrder = o, ct);
        return RedirectToAction(nameof(Index));
    }
}
