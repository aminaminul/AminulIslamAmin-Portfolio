using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class ProjectsController : AdminControllerBase
{
    private readonly ISlugService _slugs;
    private readonly IPortfolioQueryService _query;
    private readonly ISiteSettingsService _settings;

    public ProjectsController(Data.AppDbContext db, IMediaService media, ISlugService slugs,
        IPortfolioQueryService query, ISiteSettingsService settings)
        : base(db, media)
    {
        _slugs = slugs;
        _query = query;
        _settings = settings;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? status, string? category, int page = 1, CancellationToken ct = default)
    {
        var query = Db.Projects.AsNoTracking().Include(p => p.Technologies).ThenInclude(t => t.Technology).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Title.Contains(q.Trim()) || p.Slug.Contains(q.Trim()));
        if (status == "published") query = query.Where(p => p.Status == ContentStatus.Published);
        else if (status == "draft") query = query.Where(p => p.Status == ContentStatus.Draft);
        else if (status == "archived") query = query.Where(p => p.Status == ContentStatus.Archived);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(p => p.Category == category);

        var items = await PagedList<Project>.CreateAsync(
            query.OrderBy(p => p.DisplayOrder).ThenByDescending(p => p.UpdatedAt), page, AdminPageSize, ct);

        var model = new AdminListViewModel<Project>
        {
            Items = items.Items,
            Q = q,
            Status = status,
            Filter = category,
            Pager = Pager(items.Page, items.TotalPages, p => Url.Action(nameof(Index), new { q, status, category, page = p })!)
        };
        ViewData["Categories"] = await Db.Projects.AsNoTracking()
            .Where(p => p.Category != null && p.Category != "")
            .Select(p => p.Category!).Distinct().OrderBy(c => c).ToListAsync(ct);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await LoadImageChoicesAsync(ct, "CoverImage");
        var next = await Db.Projects.CountAsync(ct) + 1;
        return View("Form", new ProjectFormModel { DisplayOrder = next, Status = ContentStatus.Draft, Stage = ProjectStage.InProgress });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Create(ProjectFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadImageChoicesAsync(ct, "CoverImage");
            return View("Form", form);
        }

        try
        {
            form.CoverImageMediaId = await KeepOrReplaceAsync(form.CoverImageFile, form.CoverImageMediaId, MediaKind.Image, form.Title, ct);
            var entity = new Project();
            form.ApplyTo(entity);
            entity.Slug = await UniqueSlugAsync(form, 0, ct);
            SetPublishedAt(entity, form.Status);
            Db.Projects.Add(entity);
            await SyncTechnologiesAsync(entity, form.TechnologiesCsv, ct);
            await AddGalleryAsync(entity, form, ct);
            await Db.SaveChangesAsync(ct);
            NotifySuccess($"Project \"{entity.Title}\" created.");
            return RedirectToAction(nameof(Index));
        }
        catch (MediaValidationException)
        {
            await LoadImageChoicesAsync(ct, "CoverImage");
            return View("Form", form);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await Db.Projects.AsNoTracking()
            .Include(p => p.Technologies).ThenInclude(t => t.Technology)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return NotFound();
        await LoadImageChoicesAsync(ct, "CoverImage");
        return View("Form", ProjectFormModel.From(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, ProjectFormModel form, CancellationToken ct)
    {
        if (id != form.Id) return BadRequest();
        var entity = await Db.Projects
            .Include(p => p.Technologies).ThenInclude(t => t.Technology)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadImageChoicesAsync(ct, "CoverImage");
            return View("Form", form);
        }

        try
        {
            form.CoverImageMediaId = await KeepOrReplaceAsync(form.CoverImageFile, form.CoverImageMediaId, MediaKind.Image, form.Title, ct);
            form.ApplyTo(entity);
            entity.Slug = await UniqueSlugAsync(form, id, ct);
            SetPublishedAt(entity, form.Status);
            await SyncTechnologiesAsync(entity, form.TechnologiesCsv, ct);
            await AddGalleryAsync(entity, form, ct);
            await Db.SaveChangesAsync(ct);
            NotifySuccess("Project saved.");
            return RedirectToAction(nameof(Index));
        }
        catch (MediaValidationException)
        {
            await LoadImageChoicesAsync(ct, "CoverImage");
            return View("Form", form);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return NotFound();
        Db.Projects.Remove(entity);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Project deleted.");
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
        var entity = await Db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsFeatured = !entity.IsFeatured;
        await Db.SaveChangesAsync(ct);
        NotifySuccess(entity.IsFeatured ? "Project featured on the home page." : "Project removed from featured.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction, CancellationToken ct)
    {
        var items = await Db.Projects.ToListAsync(ct);
        await MoveAsync(items, id, direction, x => x.Id, x => x.DisplayOrder, (x, o) => x.DisplayOrder = o, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Images(int id, CancellationToken ct)
    {
        var project = await Db.Projects.AsNoTracking()
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder)).ThenInclude(i => i.MediaFile)
            .Include(p => p.CoverImage)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null) return NotFound();
        await LoadImageChoicesAsync(ct, "NewImage");
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> UploadImages(int id, List<IFormFile>? files, CancellationToken ct)
    {
        var project = await Db.Projects.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null) return NotFound();

        if (files is null || files.All(f => f.Length == 0))
        {
            NotifyError("Choose at least one image to upload.");
            return RedirectToAction(nameof(Images), new { id });
        }

        var order = project.Images.Count + 1;
        var saved = 0;
        foreach (var file in files.Where(f => f.Length > 0).Take(10))
        {
            try
            {
                var media = await Media.SaveAsync(file, MediaKind.Image, project.Title, CurrentUserName, ct);
                project.Images.Add(new ProjectImage { MediaFileId = media.Id, DisplayOrder = order++ });
                saved++;
            }
            catch (MediaValidationException ex)
            {
                NotifyError(ex.Message);
            }
        }

        await Db.SaveChangesAsync(ct);
        if (saved > 0) NotifySuccess($"{saved} gallery image(s) uploaded.");
        return RedirectToAction(nameof(Images), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCover(int id, int mediaId, CancellationToken ct)
    {
        var project = await Db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null) return NotFound();
        project.CoverImageMediaId = mediaId;
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Cover image updated.");
        return RedirectToAction(nameof(Images), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveImage(int id, int imageId, CancellationToken ct)
    {
        var image = await Db.ProjectImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProjectId == id, ct);
        if (image is null) return NotFound();
        Db.ProjectImages.Remove(image);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Gallery image removed. The file remains in the media library.");
        return RedirectToAction(nameof(Images), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int id, CancellationToken ct)
    {
        var project = await Db.Projects.AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.CoverImage)
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder)).ThenInclude(i => i.MediaFile)
            .Include(p => p.Technologies).ThenInclude(t => t.Technology)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null) return NotFound();

        var model = new ProjectDetailViewModel
        {
            Settings = await _settings.GetAsync(ct),
            Project = project,
            Related = await _query.GetRelatedProjectsAsync(project, 3, ct)
        };
        ViewData["IsPreview"] = true;
        ViewData["PreviewTitle"] = project.Title;
        return View("~/Views/Projects/Details.cshtml", model);
    }

    private async Task<IActionResult> SetStatusAsync(int id, ContentStatus status, CancellationToken ct)
    {
        var entity = await Db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return NotFound();
        SetPublishedAt(entity, status);
        await Db.SaveChangesAsync(ct);
        NotifySuccess(status switch
        {
            ContentStatus.Published => "Project published. It is now visible on the public website.",
            ContentStatus.Archived => "Project archived and hidden from the public website.",
            _ => "Project moved back to draft."
        });
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> UniqueSlugAsync(ProjectFormModel form, int excludeId, CancellationToken ct)
    {
        var baseSlug = _slugs.Create(form.Slug, form.Title, "project");
        return await _slugs.EnsureUniqueAsync(baseSlug,
            s => Db.Projects.AnyAsync(p => p.Slug == s && p.Id != excludeId, ct), ct);
    }

    private async Task SyncTechnologiesAsync(Project project, string? csv, CancellationToken ct)
    {
        var names = string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase).Take(15).ToList();

        var desired = new List<Technology>();
        foreach (var name in names)
        {
            var slug = _slugs.Create(null, name, "tech");
            var tech = await Db.Technologies.FirstOrDefaultAsync(t => t.Name == name || t.Slug == slug, ct)
                       ?? desired.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || t.Slug == slug);
            if (tech is null)
            {
                tech = new Technology { Name = name, Slug = slug };
                Db.Technologies.Add(tech);
            }
            desired.Add(tech);
        }

        foreach (var link in project.Technologies.ToList())
        {
            var matches = link.Technology is not null && desired.Any(t => t == link.Technology || (t.Id != 0 && t.Id == link.TechnologyId));
            if (!matches) project.Technologies.Remove(link);
        }

        foreach (var tech in desired)
        {
            if (!project.Technologies.Any(l => l.Technology == tech || (tech.Id != 0 && l.TechnologyId == tech.Id)))
                project.Technologies.Add(new ProjectTechnology { Technology = tech });
        }
    }

    private async Task AddGalleryAsync(Project project, ProjectFormModel form, CancellationToken ct)
    {
        var uploads = form.GalleryUploads ?? [];
        if (uploads.Count == 0) return;
        var order = project.Images.Count + 1;
        foreach (var file in uploads.Where(f => f.Length > 0).Take(10))
        {
            var media = await Media.SaveAsync(file, MediaKind.Image, project.Title, CurrentUserName, ct);
            project.Images.Add(new ProjectImage { MediaFileId = media.Id, DisplayOrder = order++ });
        }
    }
}
