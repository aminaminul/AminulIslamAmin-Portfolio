using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class SkillsController : AdminControllerBase
{
    public SkillsController(Data.AppDbContext db, IMediaService media) : base(db, media) { }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int? categoryId, CancellationToken ct)
    {
        var query = Db.Skills.AsNoTracking().Include(s => s.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(s => s.Name.Contains(q.Trim()));
        if (categoryId is not null) query = query.Where(s => s.CategoryId == categoryId);
        var items = await query.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Id).ToListAsync(ct);

        ViewData["Categories"] = await Db.SkillCategories.AsNoTracking().OrderBy(c => c.DisplayOrder).ToListAsync(ct);
        ViewData["Q"] = q;
        ViewData["CategoryId"] = categoryId;
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await LoadCategorySelectAsync(ct);
        var next = await Db.Skills.CountAsync(ct) + 1;
        return View("Form", new SkillFormModel { DisplayOrder = next, IsVisible = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SkillFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategorySelectAsync(ct);
            return View("Form", form);
        }

        var entity = new Skill();
        form.ApplyTo(entity);
        Db.Skills.Add(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Skill created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await Db.Skills.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null) return NotFound();
        await LoadCategorySelectAsync(ct);
        return View("Form", SkillFormModel.From(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SkillFormModel form, CancellationToken ct)
    {
        if (id != form.Id) return BadRequest();
        var entity = await Db.Skills.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null) return NotFound();
        if (!ModelState.IsValid)
        {
            await LoadCategorySelectAsync(ct);
            return View("Form", form);
        }

        form.ApplyTo(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Skill updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.Skills.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null) return NotFound();
        Db.Skills.Remove(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Skill deleted.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction, CancellationToken ct)
    {
        var items = await Db.Skills.ToListAsync(ct);
        await MoveAsync(items, id, direction, x => x.Id, x => x.DisplayOrder, (x, o) => x.DisplayOrder = o, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Categories(CancellationToken ct)
    {
        var items = await Db.SkillCategories.AsNoTracking()
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id).ToListAsync(ct);
        // Keyed by int (not int?): uncategorised skills have no row in this list.
        ViewData["SkillCounts"] = await Db.Skills.AsNoTracking()
            .Where(s => s.CategoryId != null)
            .GroupBy(s => s.CategoryId!.Value).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        return View(items);
    }

    [HttpGet]
    public IActionResult CategoryCreate()
    {
        return View("CategoryForm", new SkillCategoryFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryCreate(SkillCategoryFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("CategoryForm", form);
        var entity = new SkillCategory
        {
            Name = form.Name.Trim(),
            NameBn = string.IsNullOrWhiteSpace(form.NameBn) ? null : form.NameBn.Trim(),
            DisplayOrder = form.DisplayOrder,
            IsVisible = form.IsVisible
        };
        Db.SkillCategories.Add(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Skill category created.");
        return RedirectToAction(nameof(Categories));
    }

    [HttpGet]
    public async Task<IActionResult> CategoryEdit(int id, CancellationToken ct)
    {
        var entity = await Db.SkillCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return NotFound();
        return View("CategoryForm", SkillCategoryFormModel.From(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryEdit(int id, SkillCategoryFormModel form, CancellationToken ct)
    {
        if (id != form.Id) return BadRequest();
        var entity = await Db.SkillCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return NotFound();
        if (!ModelState.IsValid) return View("CategoryForm", form);

        entity.Name = form.Name.Trim();
        entity.NameBn = string.IsNullOrWhiteSpace(form.NameBn) ? null : form.NameBn.Trim();
        entity.DisplayOrder = form.DisplayOrder;
        entity.IsVisible = form.IsVisible;
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Skill category updated.");
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryDelete(int id, CancellationToken ct)
    {
        var entity = await Db.SkillCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return NotFound();

        var hasSkills = await Db.Skills.AnyAsync(s => s.CategoryId == id, ct);
        if (hasSkills)
        {
            NotifyError("Move or delete the skills in this category before deleting it.");
            return RedirectToAction(nameof(Categories));
        }

        Db.SkillCategories.Remove(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Skill category deleted.");
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryMove(int id, int direction, CancellationToken ct)
    {
        var items = await Db.SkillCategories.ToListAsync(ct);
        await MoveAsync(items, id, direction, x => x.Id, x => x.DisplayOrder, (x, o) => x.DisplayOrder = o, ct);
        return RedirectToAction(nameof(Categories));
    }

    private async Task LoadCategorySelectAsync(CancellationToken ct)
    {
        ViewData["CategoryOptions"] = await Db.SkillCategories.AsNoTracking()
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync(ct);
    }
}
