using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class ResearchController : AdminControllerBase
{
    private readonly ISlugService _slugs;
    private readonly IPortfolioQueryService _query;
    private readonly ISiteSettingsService _settings;

    public ResearchController(Data.AppDbContext db, IMediaService media, ISlugService slugs,
        IPortfolioQueryService query, ISiteSettingsService settings) : base(db, media)
    {
        _slugs = slugs;
        _query = query;
        _settings = settings;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? status, string? category, int page = 1, CancellationToken ct = default)
    {
        var query = Db.Research.AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Authors)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(r => r.Title.Contains(q.Trim()) || r.Slug.Contains(q.Trim()));
        if (status == "published") query = query.Where(r => r.Status == ContentStatus.Published);
        else if (status == "draft") query = query.Where(r => r.Status == ContentStatus.Draft);
        else if (status == "archived") query = query.Where(r => r.Status == ContentStatus.Archived);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(r => r.Category != null && r.Category.Slug == category);

        var items = await PagedList<Research>.CreateAsync(
            query.OrderBy(r => r.DisplayOrder).ThenByDescending(r => r.UpdatedAt), page, AdminPageSize, ct);

        var model = new AdminListViewModel<Research>
        {
            Items = items.Items,
            Q = q,
            Status = status,
            Filter = category,
            Pager = Pager(items.Page, items.TotalPages, p => Url.Action(nameof(Index), new { q, status, category, page = p })!)
        };
        ViewData["ResearchCategories"] = await Db.ResearchCategories.AsNoTracking()
            .OrderBy(c => c.DisplayOrder).ToListAsync(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await PrepareFormAsync(null, ct);
        var next = await Db.Research.CountAsync(ct) + 1;
        return View("Form", new ResearchFormModel { DisplayOrder = next, Status = ContentStatus.Draft });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Create(ResearchFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PrepareFormAsync(null, ct);
            return View("Form", form);
        }

        try
        {
            form.CoverImageMediaId = await KeepOrReplaceAsync(form.CoverImageFile, form.CoverImageMediaId, MediaKind.Image, form.Title, ct);
            form.PdfMediaId = await KeepOrReplaceAsync(form.PdfFile, form.PdfMediaId, MediaKind.Document, form.Title + " (PDF)", ct);
            var entity = new Research();
            Db.Research.Add(entity);
            await SaveAsync(entity, form, ct);
            NotifySuccess("Research record created.");
            return RedirectToAction(nameof(Index));
        }
        catch (MediaValidationException)
        {
            await PrepareFormAsync(null, ct);
            return View("Form", form);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await Db.Research.AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Authors)
            .Include(r => r.Tags).ThenInclude(t => t.Tag)
            .Include(r => r.Related)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return NotFound();

        await PrepareFormAsync(id, ct);
        return View("Form", ResearchFormModel.From(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, ResearchFormModel form, CancellationToken ct)
    {
        if (id != form.Id) return BadRequest();
        var entity = await Db.Research
            .Include(r => r.Authors)
            .Include(r => r.Tags).ThenInclude(t => t.Tag)
            .Include(r => r.Related)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await PrepareFormAsync(id, ct);
            return View("Form", form);
        }

        try
        {
            form.CoverImageMediaId = await KeepOrReplaceAsync(form.CoverImageFile, form.CoverImageMediaId, MediaKind.Image, form.Title, ct);
            form.PdfMediaId = await KeepOrReplaceAsync(form.PdfFile, form.PdfMediaId, MediaKind.Document, form.Title + " (PDF)", ct);
            await SaveAsync(entity, form, ct);
            NotifySuccess("Research record saved.");
            return RedirectToAction(nameof(Index));
        }
        catch (MediaValidationException)
        {
            await PrepareFormAsync(id, ct);
            return View("Form", form);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.Research.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return NotFound();
        Db.Research.Remove(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Research record deleted.");
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
    public async Task<IActionResult> ToggleFeatured(int id, CancellationToken ct)
    {
        var entity = await Db.Research.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsFeatured = !entity.IsFeatured;
        await Db.SaveChangesAsync(ct);
        NotifySuccess(entity.IsFeatured ? "Marked as featured." : "Removed from featured.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction, CancellationToken ct)
    {
        var items = await Db.Research.ToListAsync(ct);
        await MoveAsync(items, id, direction, x => x.Id, x => x.DisplayOrder, (x, o) => x.DisplayOrder = o, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int id, CancellationToken ct)
    {
        var research = await Db.Research.AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.CoverImage)
            .Include(r => r.Pdf)
            .Include(r => r.Authors.OrderBy(a => a.DisplayOrder))
            .Include(r => r.Tags).ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        if (research is null) return NotFound();

        var model = new ResearchDetailViewModel
        {
            Settings = await _settings.GetAsync(ct),
            Research = research,
            Related = await _query.GetRelatedResearchAsync(research, 3, ct)
        };
        ViewData["IsPreview"] = true;
        ViewData["PreviewTitle"] = research.Title;
        return View("~/Views/Research/Details.cshtml", model);
    }

    // ---------- Research categories ----------

    [HttpGet]
    public async Task<IActionResult> Categories(CancellationToken ct)
    {
        var items = await Db.ResearchCategories.AsNoTracking()
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id).ToListAsync(ct);
        // Keyed by int (not int?): uncategorised records have no row in this list.
        ViewData["Usage"] = await Db.Research.AsNoTracking()
            .Where(r => r.CategoryId != null)
            .GroupBy(r => r.CategoryId!.Value).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> CategoryCreate(CancellationToken ct)
    {
        var next = await Db.ResearchCategories.CountAsync(ct) + 1;
        return View("CategoryForm", new ResearchCategoryFormModel { DisplayOrder = next, IsVisible = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryCreate(ResearchCategoryFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("CategoryForm", form);
        var entity = new ResearchCategory
        {
            Name = form.Name.Trim(),
            NameBn = string.IsNullOrWhiteSpace(form.NameBn) ? null : form.NameBn.Trim(),
            DisplayOrder = form.DisplayOrder,
            IsVisible = form.IsVisible
        };
        entity.Slug = await _slugs.EnsureUniqueAsync(
            _slugs.Create(form.Slug, form.Name, "category"),
            s => Db.ResearchCategories.AnyAsync(c => c.Slug == s, ct), ct);
        Db.ResearchCategories.Add(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Research category created.");
        return RedirectToAction(nameof(Categories));
    }

    [HttpGet]
    public async Task<IActionResult> CategoryEdit(int id, CancellationToken ct)
    {
        var entity = await Db.ResearchCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return NotFound();
        return View("CategoryForm", new ResearchCategoryFormModel
        {
            Id = entity.Id,
            Name = entity.Name,
            NameBn = entity.NameBn,
            Slug = entity.Slug,
            DisplayOrder = entity.DisplayOrder,
            IsVisible = entity.IsVisible
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryEdit(int id, ResearchCategoryFormModel form, CancellationToken ct)
    {
        if (id != form.Id) return BadRequest();
        var entity = await Db.ResearchCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return NotFound();
        if (!ModelState.IsValid) return View("CategoryForm", form);

        entity.Name = form.Name.Trim();
        entity.NameBn = string.IsNullOrWhiteSpace(form.NameBn) ? null : form.NameBn.Trim();
        entity.DisplayOrder = form.DisplayOrder;
        entity.IsVisible = form.IsVisible;
        if (!string.IsNullOrWhiteSpace(form.Slug))
        {
            entity.Slug = await _slugs.EnsureUniqueAsync(
                _slugs.Create(form.Slug, form.Name, "category"),
                s => Db.ResearchCategories.AnyAsync(c => c.Slug == s && c.Id != id, ct), ct);
        }
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Research category updated.");
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryDelete(int id, CancellationToken ct)
    {
        var entity = await Db.ResearchCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return NotFound();
        if (await Db.Research.AnyAsync(r => r.CategoryId == id, ct))
        {
            NotifyError("This category is still used by research records. Reassign them first.");
            return RedirectToAction(nameof(Categories));
        }
        Db.ResearchCategories.Remove(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Research category deleted.");
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryMove(int id, int direction, CancellationToken ct)
    {
        var items = await Db.ResearchCategories.ToListAsync(ct);
        await MoveAsync(items, id, direction, x => x.Id, x => x.DisplayOrder, (x, o) => x.DisplayOrder = o, ct);
        return RedirectToAction(nameof(Categories));
    }

    private async Task PrepareFormAsync(int? excludeId, CancellationToken ct)
    {
        await LoadImageChoicesAsync(ct, "CoverImage");
        await LoadChoicesAsync("Pdf", MediaKind.Document, ct);
        ViewData["ResearchCategories"] = await Db.ResearchCategories.AsNoTracking()
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync(ct);
        ViewData["RelatedChoices"] = await PublishedResearchChoicesAsync(excludeId, ct);
    }

    private async Task<string> UniqueSlugAsync(ResearchFormModel form, int excludeId, CancellationToken ct)
    {
        var baseSlug = _slugs.Create(form.Slug, form.Title, "research");
        return await _slugs.EnsureUniqueAsync(baseSlug,
            s => Db.Research.AnyAsync(r => r.Slug == s && r.Id != excludeId, ct), ct);
    }

    private async Task SaveAsync(Research entity, ResearchFormModel form, CancellationToken ct)
    {
        entity.Slug = await UniqueSlugAsync(form, entity.Id, ct);
        form.ApplyTo(entity);
        SetPublishedAt(entity, form.Status);

        entity.Authors.Clear();
        var authorOrder = 1;
        foreach (var author in form.ParseAuthors())
        {
            entity.Authors.Add(new ResearchAuthor
            {
                Name = author.Name.Trim(),
                Affiliation = author.Affiliation,
                Role = author.Role,
                DisplayOrder = authorOrder++
            });
        }

        entity.Tags.Clear();
        foreach (var tagName in form.ParseTags())
        {
            var trimmed = tagName.Trim();
            var slug = _slugs.Create(null, trimmed, "tag");
            var tag = await Db.Tags.FirstOrDefaultAsync(t => t.Name == trimmed || t.Slug == slug, ct);
            if (tag is null)
            {
                tag = new Tag { Name = trimmed, Slug = slug };
                Db.Tags.Add(tag);
            }
            entity.Tags.Add(new ResearchTag { Tag = tag });
        }

        entity.Related.Clear();
        var candidateIds = form.RelatedIds.Distinct().Where(x => x != entity.Id).Take(6).ToList();
        if (candidateIds.Count > 0)
        {
            var validIds = await Db.Research.AsNoTracking()
                .Where(r => candidateIds.Contains(r.Id))
                .Select(r => r.Id)
                .ToListAsync(ct);
            foreach (var relatedId in validIds)
                entity.Related.Add(new ResearchRelated { RelatedResearchId = relatedId });
        }

        await Db.SaveChangesAsync(ct);
    }

    private async Task<IActionResult> SetStatusAsync(int id, ContentStatus status, CancellationToken ct)
    {
        var entity = await Db.Research.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return NotFound();
        SetPublishedAt(entity, status);
        await Db.SaveChangesAsync(ct);
        NotifySuccess(status switch
        {
            ContentStatus.Published => "Research published. It is now visible to visitors.",
            ContentStatus.Archived => "Research archived and removed from public pages.",
            _ => "Research moved back to draft."
        });
        return RedirectToAction(nameof(Index));
    }
}
