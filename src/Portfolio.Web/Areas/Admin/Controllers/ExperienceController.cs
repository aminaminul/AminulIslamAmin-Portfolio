using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class ExperienceController : AdminControllerBase
{
    public ExperienceController(Data.AppDbContext db, IMediaService media) : base(db, media) { }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? status, int page = 1, CancellationToken ct = default)
    {
        var query = Db.Experiences.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.JobTitle.Contains(q.Trim()) || x.Company.Contains(q.Trim()));
        if (status == "published") query = query.Where(x => x.Status == ContentStatus.Published);
        else if (status == "draft") query = query.Where(x => x.Status == ContentStatus.Draft);
        else if (status == "archived") query = query.Where(x => x.Status == ContentStatus.Archived);

        var items = await PagedList<Experience>.CreateAsync(
            query.OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.StartDate), page, AdminPageSize, ct);

        return View(new AdminListViewModel<Experience>
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
        var next = await Db.Experiences.CountAsync(ct) + 1;
        return View("Form", new ExperienceFormModel { DisplayOrder = next, Status = ContentStatus.Draft, StartDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExperienceFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("Form", form);
        var entity = new Experience();
        form.ApplyTo(entity);
        SetPublishedAt(entity, form.Status);
        Db.Experiences.Add(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Experience record created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await Db.Experiences.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        return View("Form", ExperienceFormModel.From(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ExperienceFormModel form, CancellationToken ct)
    {
        if (id != form.Id) return BadRequest();
        var entity = await Db.Experiences.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (!ModelState.IsValid) return View("Form", form);

        form.ApplyTo(entity);
        SetPublishedAt(entity, form.Status);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Experience updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.Experiences.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        Db.Experiences.Remove(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Experience deleted.");
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
        var items = await Db.Experiences.ToListAsync(ct);
        await MoveAsync(items, id, direction, x => x.Id, x => x.DisplayOrder, (x, o) => x.DisplayOrder = o, ct);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SetStatusAsync(int id, ContentStatus status, CancellationToken ct)
    {
        var entity = await Db.Experiences.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        SetPublishedAt(entity, status);
        await Db.SaveChangesAsync(ct);
        NotifySuccess(status == ContentStatus.Published ? "Experience published." : "Experience moved to draft.");
        return RedirectToAction(nameof(Index));
    }
}
