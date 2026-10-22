using Microsoft.AspNetCore.Mvc;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Services;

namespace Portfolio.Web.Controllers;

public class ProjectsController : Controller
{
    private readonly IPortfolioQueryService _query;
    private readonly ISiteSettingsService _settings;
    private readonly ISiteChromeService _chrome;

    public ProjectsController(IPortfolioQueryService query, ISiteSettingsService settings, ISiteChromeService chrome)
    {
        _query = query;
        _settings = settings;
        _chrome = chrome;
    }

    [HttpGet("/projects")]
    public async Task<IActionResult> Index(string? q, string? category, string? tech, int page = 1, CancellationToken ct = default)
    {
        var settings = await _settings.GetAsync(ct);
        var model = new ProjectsIndexViewModel
        {
            Settings = settings,
            Projects = await _query.GetProjectsAsync(q, category, tech, page, ct),
            Categories = await _query.GetProjectCategoriesAsync(ct),
            Query = q,
            Category = category,
            Tech = tech
        };

        var title = !string.IsNullOrWhiteSpace(tech)
            ? $"{tech} Projects"
            : (!string.IsNullOrWhiteSpace(category) ? $"{category} Projects" : "Projects");

        var description = $"Projects by {HomeController.DisplayName(await _query.GetProfileAsync(ct), settings)}";
        ViewData["Seo"] = new SeoModel(
            title,
            description,
            CanonicalPath: "/projects");
        return View(model);
    }

    [HttpGet("/projects/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken ct)
    {
        var project = await _query.GetPublishedProjectAsync(slug, ct);
        if (project is null) return NotFound();

        var settings = await _settings.GetAsync(ct);
        var model = new ProjectDetailViewModel
        {
            Settings = settings,
            Project = project,
            Related = await _query.GetRelatedProjectsAsync(project, 3, ct)
        };

        ViewData["Seo"] = new SeoModel(
            string.IsNullOrWhiteSpace(project.SeoTitle) ? project.Title : project.SeoTitle,
            string.IsNullOrWhiteSpace(project.SeoDescription) ? project.Summary : project.SeoDescription,
            project.CoverImage is null ? null : Url.Action("Index", "Media", new { id = project.CoverImage.Id }),
            CanonicalPath: $"/projects/{project.Slug}",
            Type: "article");
        return View(model);
    }
}
