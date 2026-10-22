using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class TestimonialsController : AdminControllerBase
{
    public TestimonialsController(Data.AppDbContext db, IMediaService media) : base(db, media) { }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? status, int page = 1, CancellationToken ct = default)
    {
        var query = Db.Testimonials.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.ClientName.Contains(q.Trim()) || x.Quote.Contains(q.Trim()));
        if (status == "published") query = query.Where(x => x.Status == ContentStatus.Published);
        else if (status == "draft") query = query.Where(x => x.Status == ContentStatus.Draft);
        else if (status == "archived") query = query.Where(x => x.Status == ContentStatus.Archived);

        var items = await PagedList<Testimonial>.CreateAsync(
            query.OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.CreatedAt), page, AdminPageSize, ct);

        return View(new AdminListViewModel<Testimonial>
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
        await LoadImageChoicesAsync(ct, "Avatar");
        var next = await Db.Testimonials.CountAsync(ct) + 1;
        return View("Form", new TestimonialFormModel { DisplayOrder = next, Status = ContentStatus.Draft });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TestimonialFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadImageChoicesAsync(ct, "Avatar");
            return View("Form", form);
        }
        try
        {
            form.AvatarMediaId = await KeepOrReplaceAsync(form.AvatarFile, form.AvatarMediaId, MediaKind.Image, form.ClientName, ct);
        }
        catch (MediaValidationException)
        {
            await LoadImageChoicesAsync(ct, "Avatar");
            return View("Form", form);
        }

        var entity = new Testimonial();
        form.ApplyTo(entity);
        SetPublishedAt(entity, form.Status);
        Db.Testimonials.Add(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Testimonial created. Publish it to show it on the public site.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await Db.Testimonials.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        await LoadImageChoicesAsync(ct, "Avatar");
        return View("Form", TestimonialFormModel.From(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TestimonialFormModel form, CancellationToken ct)
    {
        if (id != form.Id) return BadRequest();
        var entity = await Db.Testimonials.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (!ModelState.IsValid)
        {
            await LoadImageChoicesAsync(ct, "Avatar");
            return View("Form", form);
        }
        try
        {
            form.AvatarMediaId = await KeepOrReplaceAsync(form.AvatarFile, form.AvatarMediaId, MediaKind.Image, form.ClientName, ct);
        }
        catch (MediaValidationException)
        {
            await LoadImageChoicesAsync(ct, "Avatar");
            return View("Form", form);
        }

        form.ApplyTo(entity);
        SetPublishedAt(entity, form.Status);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Testimonial updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.Testimonials.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        Db.Testimonials.Remove(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Testimonial deleted.");
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
    public async Task<IActionResult> Archive(int id, CancellationToken ct) => await SetStatusAsync(id, ContentStatus.Archived, ct);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction, CancellationToken ct)
    {
        var items = await Db.Testimonials.ToListAsync(ct);
        await MoveAsync(items, id, direction, x => x.Id, x => x.DisplayOrder, (x, o) => x.DisplayOrder = o, ct);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SetStatusAsync(int id, ContentStatus status, CancellationToken ct)
    {
        var entity = await Db.Testimonials.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        SetPublishedAt(entity, status);
        await Db.SaveChangesAsync(ct);
        NotifySuccess(status switch
        {
            ContentStatus.Published => "Testimonial approved and published.",
            ContentStatus.Archived => "Testimonial archived.",
            _ => "Testimonial unpublished."
        });
        return RedirectToAction(nameof(Index));
    }
}
