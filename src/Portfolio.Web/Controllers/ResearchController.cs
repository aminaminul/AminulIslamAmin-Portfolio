using Microsoft.AspNetCore.Mvc;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Services;

namespace Portfolio.Web.Controllers;

public class ResearchController : Controller
{
    private readonly IPortfolioQueryService _query;
    private readonly ISiteSettingsService _settings;

    public ResearchController(IPortfolioQueryService query, ISiteSettingsService settings)
    {
        _query = query;
        _settings = settings;
    }

    [HttpGet("/research")]
    public async Task<IActionResult> Index(string? q, string? category, string? tag, int? year, int page = 1, CancellationToken ct = default)
    {
        var settings = await _settings.GetAsync(ct);
        var model = new ResearchIndexViewModel
        {
            Settings = settings,
            Records = await _query.GetResearchAsync(q, category, tag, year, page, ct),
            Categories = await _query.GetResearchCategoriesAsync(ct),
            Years = await _query.GetResearchYearsAsync(ct),
            Query = q,
            Category = category,
            Tag = tag,
            Year = year
        };

        ViewData["Seo"] = new SeoModel("Research & publications", settings.SeoDescription, CanonicalPath: "/research");
        return View(model);
    }

    [HttpGet("/research/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken ct)
    {
        var research = await _query.GetPublishedResearchAsync(slug, ct);
        if (research is null) return NotFound();

        var settings = await _settings.GetAsync(ct);
        var model = new ResearchDetailViewModel
        {
            Settings = settings,
            Research = research,
            Related = await _query.GetRelatedResearchAsync(research, 3, ct)
        };

        ViewData["Seo"] = new SeoModel(
            string.IsNullOrWhiteSpace(research.SeoTitle) ? research.Title : research.SeoTitle,
            string.IsNullOrWhiteSpace(research.SeoDescription) ? research.Abstract : research.SeoDescription,
            research.CoverImage is null ? null : Url.Action("Index", "Media", new { id = research.CoverImage.Id }),
            CanonicalPath: $"/research/{research.Slug}",
            Type: "article");
        return View(model);
    }
}
