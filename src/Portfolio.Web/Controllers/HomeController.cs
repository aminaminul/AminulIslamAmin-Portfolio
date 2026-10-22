using Microsoft.AspNetCore.Mvc;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Services;

namespace Portfolio.Web.Controllers;

public class HomeController : Controller
{
    private readonly IPortfolioQueryService _query;
    private readonly ISiteSettingsService _settings;
    private readonly ISiteChromeService _chrome;

    public HomeController(IPortfolioQueryService query, ISiteSettingsService settings, ISiteChromeService chrome)
    {
        _query = query;
        _settings = settings;
        _chrome = chrome;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        var chrome = await _chrome.GetAsync(Request.Path, ct);
        var profile = await _query.GetProfileAsync(ct);
        var hero = await _query.GetHeroAsync(ct);

        var model = new HomeViewModel
        {
            Settings = settings,
            Profile = profile,
            Hero = hero,
            HeroSocialLinks = chrome.SocialLinks.Where(s => s.ShowInHero).ToList(),
            AboutSections = (await _query.GetAboutSectionsAsync(ct)).Take(2).ToList(),
            SkillCategories = await _query.GetSkillsAsync(ct),
            Projects = await _query.GetPublishedProjectsAsync(ct),
            Experience = (await _query.GetExperienceAsync(ct)).Take(3).ToList(),
            Research = await _query.GetFeaturedResearchAsync(3, ct),
            Services = (await _query.GetServicesAsync(ct)).Take(6).ToList(),
            Testimonials = (await _query.GetTestimonialsAsync(ct)).Take(3).ToList(),
            Certifications = (await _query.GetCertificationsAsync(ct)).Take(4).ToList()
        };

        ViewData["Seo"] = new SeoModel(
            settings.SeoTitle ?? $"{DisplayName(profile, settings)} — {profile.ProfessionalTitle ?? settings.Tagline}",
            settings.SeoDescription ?? profile.ShortIntro,
            CanonicalPath: "/");
        return View(model);
    }

    [HttpGet("/about")]
    public async Task<IActionResult> About(CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        var profile = await _query.GetProfileAsync(ct);
        var model = new AboutViewModel
        {
            Settings = settings,
            Profile = profile,
            Sections = await _query.GetAboutSectionsAsync(ct),
            Experience = await _query.GetExperienceAsync(ct),
            Education = await _query.GetEducationAsync(ct),
            Certifications = await _query.GetCertificationsAsync(ct),
            Achievements = await _query.GetAchievementsAsync(ct),
            SkillCategories = await _query.GetSkillsAsync(ct)
        };
        ViewData["Seo"] = new SeoModel(
            $"About — {DisplayName(profile, settings)}",
            profile.ShortIntro,
            CanonicalPath: "/about");
        return View(model);
    }

    [HttpGet("/skills")]
    public async Task<IActionResult> Skills(CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        var profile = await _query.GetProfileAsync(ct);
        var model = new SkillsViewModel
        {
            Settings = settings,
            Profile = profile,
            Categories = await _query.GetSkillsAsync(ct)
        };
        ViewData["Seo"] = new SeoModel($"Skills — {DisplayName(profile, settings)}", settings.SeoDescription, CanonicalPath: "/skills");
        return View(model);
    }

    [HttpGet("/services")]
    public async Task<IActionResult> Services(CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        var profile = await _query.GetProfileAsync(ct);
        var model = new ServicesViewModel
        {
            Settings = settings,
            Profile = profile,
            Services = await _query.GetServicesAsync(ct),
            Testimonials = await _query.GetTestimonialsAsync(ct)
        };
        ViewData["Seo"] = new SeoModel($"Services — {DisplayName(profile, settings)}", settings.SeoDescription, CanonicalPath: "/services");
        return View(model);
    }

    public static string DisplayName(Profile profile, SiteSettings settings) =>
        string.IsNullOrWhiteSpace(profile.DisplayName)
            ? string.IsNullOrWhiteSpace(profile.FullName) ? settings.SiteName : profile.FullName
            : profile.DisplayName!;
}
