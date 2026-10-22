using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;

namespace Portfolio.Web.Services;

/// <summary>Picks the Bangla value when the request culture is Bangla and a translation exists.</summary>
public static class ContentLocalization
{
    public static bool IsBangla => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "bn";

    /// <summary>Falls back to English when no translation has been supplied (documented fallback).</summary>
    public static string Pick(this string? english, string? bangla) =>
        IsBangla && !string.IsNullOrWhiteSpace(bangla) ? bangla : english ?? string.Empty;

    public static string? PickOptional(this string? english, string? bangla) =>
        IsBangla && !string.IsNullOrWhiteSpace(bangla) ? bangla : english;

    public static string FormatDate(this DateTime? date) =>
        date is null ? string.Empty : date.Value.ToString("MMM yyyy", CultureInfo.CurrentCulture);

    public static string FormatDateFull(this DateTime? date) =>
        date is null ? string.Empty : date.Value.ToString("d MMMM yyyy", CultureInfo.CurrentCulture);

    /// <summary>Display name falls back to full name, then the configured site name.</summary>
    public static string DisplayName(this Profile profile, SiteSettings settings) =>
        string.IsNullOrWhiteSpace(profile.DisplayName)
            ? string.IsNullOrWhiteSpace(profile.FullName) ? settings.SiteName : profile.FullName
            : profile.DisplayName!;

    public static string FormatDateRange(this DateTime start, DateTime? end, bool isCurrent) =>
        isCurrent
            ? $"{start.ToString("MMM yyyy", CultureInfo.CurrentCulture)} – {Localized("Present")}"
            : $"{start.ToString("MMM yyyy", CultureInfo.CurrentCulture)} – {end.FormatDate()}";

    private static string Localized(string english) =>
        IsBangla ? english switch
        {
            "Present" => "বর্তমান",
            _ => english
        } : english;
}

public record ChromeData(
    SiteSettings Settings,
    IReadOnlyList<NavigationItem> Navigation,
    IReadOnlyList<NavigationItem> FooterNavigation,
    IReadOnlyList<SocialLink> SocialLinks,
    Profile Profile,
    string? CurrentPath);

public interface ISiteChromeService
{
    Task<ChromeData> GetAsync(string? currentPath, CancellationToken ct = default);
}

public class SiteChromeService : ISiteChromeService
{
    private readonly AppDbContext _db;
    private readonly ISiteSettingsService _settings;
    private ChromeData? _cached;
    private string? _cachedPath;

    public SiteChromeService(AppDbContext db, ISiteSettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<ChromeData> GetAsync(string? currentPath, CancellationToken ct = default)
    {
        // One header and one footer are rendered per request; reuse the same data.
        if (_cached is not null && _cachedPath == currentPath) return _cached;

        var settings = await _settings.GetAsync(ct);
        var profile = await _db.Profiles.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(ct) ?? new Profile();
        var nav = await _db.NavigationItems.AsNoTracking()
            .Where(x => x.IsVisible && !x.ShowInFooter)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id)
            .Take(20)
            .ToListAsync(ct);
        var footerNav = await _db.NavigationItems.AsNoTracking()
            .Where(x => x.IsVisible && x.ShowInFooter)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id)
            .Take(20)
            .ToListAsync(ct);
        var social = await _db.SocialLinks.AsNoTracking()
            .Where(x => x.IsVisible && x.ShowInFooter)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id)
            .Take(12)
            .ToListAsync(ct);

        if (nav.Count == 0)
        {
            nav = DefaultNavigation();
        }

        _cached = new ChromeData(settings, nav, footerNav, social, profile, currentPath);
        _cachedPath = currentPath;
        return _cached;
    }

    public static List<NavigationItem> DefaultNavigation() =>
    [
        new() { Label = "Home", LabelBn = "হোম", Url = "/", DisplayOrder = 1, IsVisible = true },
        new() { Label = "About", LabelBn = "পরিচিতি", Url = "/about", DisplayOrder = 2, IsVisible = true },
        new() { Label = "Skills", LabelBn = "দক্ষতা", Url = "/skills", DisplayOrder = 3, IsVisible = true },
        new() { Label = "Projects", LabelBn = "প্রকল্প", Url = "/projects", DisplayOrder = 4, IsVisible = true },
        new() { Label = "Research", LabelBn = "গবেষণা", Url = "/research", DisplayOrder = 5, IsVisible = true },
        new() { Label = "Services", LabelBn = "সেবা", Url = "/services", DisplayOrder = 6, IsVisible = true },
        new() { Label = "Contact", LabelBn = "যোগাযোগ", Url = "/contact", DisplayOrder = 7, IsVisible = true }
    ];
}

/// <summary>Read-only queries used by the public website. Only published / visible records are returned.</summary>
public interface IPortfolioQueryService
{
    Task<Profile> GetProfileAsync(CancellationToken ct = default);
    Task<HeroSettings> GetHeroAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AboutSection>> GetAboutSectionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SkillCategory>> GetSkillsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Project>> GetPublishedProjectsAsync(CancellationToken ct = default);
    Task<PagedList<Project>> GetProjectsAsync(string? q, string? category, int page, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetProjectCategoriesAsync(CancellationToken ct = default);
    Task<Project?> GetPublishedProjectAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Project>> GetRelatedProjectsAsync(Project project, int take, CancellationToken ct = default);
    Task<IReadOnlyList<Experience>> GetExperienceAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Education>> GetEducationAsync(CancellationToken ct = default);
    Task<PagedList<Research>> GetResearchAsync(string? q, string? category, string? tag, int? year, int page, CancellationToken ct = default);
    Task<IReadOnlyList<Research>> GetFeaturedResearchAsync(int take, CancellationToken ct = default);
    Task<Research?> GetPublishedResearchAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Research>> GetRelatedResearchAsync(Research research, int take, CancellationToken ct = default);
    Task<IReadOnlyList<ResearchCategory>> GetResearchCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetResearchYearsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Certification>> GetCertificationsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Achievement>> GetAchievementsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Testimonial>> GetTestimonialsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SocialLink>> GetSocialLinksAsync(CancellationToken ct = default);
    Task<int> GetPublishedProjectCountAsync(CancellationToken ct = default);
}

public class PortfolioQueryService : IPortfolioQueryService
{
    public const int ProjectPageSize = 9;
    public const int ResearchPageSize = 8;

    private readonly AppDbContext _db;

    public PortfolioQueryService(AppDbContext db) => _db = db;

    public async Task<Profile> GetProfileAsync(CancellationToken ct = default) =>
        await _db.Profiles.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(ct) ?? new Profile();

    public async Task<HeroSettings> GetHeroAsync(CancellationToken ct = default) =>
        await _db.HeroSettings.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(ct) ?? new HeroSettings();

    public async Task<IReadOnlyList<AboutSection>> GetAboutSectionsAsync(CancellationToken ct = default) =>
        await _db.AboutSections.AsNoTracking()
            .Where(x => x.IsVisible && x.Status == ContentStatus.Published)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SkillCategory>> GetSkillsAsync(CancellationToken ct = default)
    {
        var categories = await _db.SkillCategories.AsNoTracking()
            .Where(c => c.IsVisible)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id)
            .Select(c => new SkillCategory
            {
                Id = c.Id,
                Name = c.Name,
                NameBn = c.NameBn,
                DisplayOrder = c.DisplayOrder,
                IsVisible = c.IsVisible,
                Skills = c.Skills.Where(s => s.IsVisible).OrderBy(s => s.DisplayOrder).ThenBy(s => s.Id).ToList()
            })
            .ToListAsync(ct);

        return categories;
    }

    public async Task<IReadOnlyList<Project>> GetPublishedProjectsAsync(CancellationToken ct = default) =>
        await _db.Projects.AsNoTracking()
            .Where(p => p.Status == ContentStatus.Published)
            .Include(p => p.Technologies).ThenInclude(t => t.Technology)
            .OrderByDescending(p => p.IsFeatured).ThenBy(p => p.DisplayOrder).ThenByDescending(p => p.PublishedAt)
            .Take(6)
            .ToListAsync(ct);

    public async Task<PagedList<Project>> GetProjectsAsync(string? q, string? category, int page, CancellationToken ct = default)
    {
        var query = _db.Projects.AsNoTracking()
            .Where(p => p.Status == ContentStatus.Published);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Title.Contains(term)
                || (p.Summary != null && p.Summary.Contains(term))
                || (p.Description != null && p.Description.Contains(term))
                || p.Technologies.Any(t => t.Technology!.Name.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        query = query
            .OrderByDescending(p => p.IsFeatured)
            .ThenBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.PublishedAt);

        return await PagedList<Project>.CreateAsync(
            query.Include(p => p.Technologies).ThenInclude(t => t.Technology), page, ProjectPageSize, ct);
    }

    public async Task<IReadOnlyList<string>> GetProjectCategoriesAsync(CancellationToken ct = default) =>
        await _db.Projects.AsNoTracking()
            .Where(p => p.Status == ContentStatus.Published && p.Category != null && p.Category != "")
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);

    public async Task<Project?> GetPublishedProjectAsync(string slug, CancellationToken ct = default) =>
        await _db.Projects.AsNoTracking()
            // Two collection navigations: split the query so the page does not fan out into a
            // cartesian product as galleries grow.
            .AsSplitQuery()
            .Include(p => p.CoverImage)
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder)).ThenInclude(i => i.MediaFile)
            .Include(p => p.Technologies).ThenInclude(t => t.Technology)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == ContentStatus.Published, ct);

    public async Task<IReadOnlyList<Project>> GetRelatedProjectsAsync(Project project, int take, CancellationToken ct = default)
    {
        var related = await _db.Projects.AsNoTracking()
            .Where(p => p.Status == ContentStatus.Published && p.Id != project.Id && p.Category == project.Category && p.Category != null)
            .OrderByDescending(p => p.IsFeatured).ThenBy(p => p.DisplayOrder)
            .Take(take)
            .ToListAsync(ct);

        if (related.Count < take)
        {
            var ids = related.Select(r => r.Id).Append(project.Id).ToList();
            var more = await _db.Projects.AsNoTracking()
                .Where(p => p.Status == ContentStatus.Published && !ids.Contains(p.Id))
                .OrderByDescending(p => p.IsFeatured).ThenBy(p => p.DisplayOrder)
                .Take(take - related.Count)
                .ToListAsync(ct);
            related.AddRange(more);
        }

        return related;
    }

    public async Task<IReadOnlyList<Experience>> GetExperienceAsync(CancellationToken ct = default) =>
        await _db.Experiences.AsNoTracking()
            .Where(x => x.Status == ContentStatus.Published)
            .OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.StartDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Education>> GetEducationAsync(CancellationToken ct = default) =>
        await _db.Educations.AsNoTracking()
            .Where(x => x.Status == ContentStatus.Published)
            .OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.EndDate ?? x.StartDate)
            .ToListAsync(ct);

    public async Task<PagedList<Research>> GetResearchAsync(string? q, string? category, string? tag, int? year, int page, CancellationToken ct = default)
    {
        var query = _db.Research.AsNoTracking().Where(r => r.Status == ContentStatus.Published);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(r => r.Title.Contains(term)
                || (r.Abstract != null && r.Abstract.Contains(term))
                || (r.Keywords != null && r.Keywords.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(r => r.Category != null && r.Category.Slug == category);

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(r => r.Tags.Any(t => t.Tag!.Slug == tag));

        if (year is not null)
            query = query.Where(r => r.PublicationDate != null && r.PublicationDate.Value.Year == year);

        query = query.OrderByDescending(r => r.IsFeatured)
            .ThenBy(r => r.DisplayOrder)
            .ThenByDescending(r => r.PublicationDate ?? r.CreatedAt);

        return await PagedList<Research>.CreateAsync(
            query.AsSplitQuery()
                 .Include(r => r.Category)
                 .Include(r => r.Authors.OrderBy(a => a.DisplayOrder))
                 .Include(r => r.Tags).ThenInclude(t => t.Tag),
            page, ResearchPageSize, ct);
    }

    public async Task<IReadOnlyList<Research>> GetFeaturedResearchAsync(int take, CancellationToken ct = default) =>
        await _db.Research.AsNoTracking()
            .Where(r => r.Status == ContentStatus.Published)
            .OrderByDescending(r => r.IsFeatured)
            .ThenByDescending(r => r.PublicationDate ?? r.CreatedAt)
            .Take(take)
            .Include(r => r.Category)
            .Include(r => r.Tags).ThenInclude(t => t.Tag)
            .ToListAsync(ct);

    public async Task<Research?> GetPublishedResearchAsync(string slug, CancellationToken ct = default) =>
        await _db.Research.AsNoTracking()
            // Three collection navigations on one record: split rather than cartesian-join.
            .AsSplitQuery()
            .Include(r => r.Category)
            .Include(r => r.CoverImage)
            .Include(r => r.Pdf)
            .Include(r => r.Authors.OrderBy(a => a.DisplayOrder))
            .Include(r => r.Tags).ThenInclude(t => t.Tag)
            .Include(r => r.Related).ThenInclude(rr => rr.RelatedResearch)
            .FirstOrDefaultAsync(r => r.Slug == slug && r.Status == ContentStatus.Published, ct);

    public async Task<IReadOnlyList<Research>> GetRelatedResearchAsync(Research research, int take, CancellationToken ct = default)
    {
        var explicitIds = research.Related.Select(r => r.RelatedResearchId).ToList();
        var related = await _db.Research.AsNoTracking()
            .Where(r => r.Status == ContentStatus.Published && r.Id != research.Id
                        && (explicitIds.Contains(r.Id)
                            || (research.CategoryId != null && r.CategoryId == research.CategoryId)))
            .OrderByDescending(r => explicitIds.Contains(r.Id))
            .ThenByDescending(r => r.PublicationDate ?? r.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
        return related;
    }

    public async Task<IReadOnlyList<ResearchCategory>> GetResearchCategoriesAsync(CancellationToken ct = default) =>
        await _db.ResearchCategories.AsNoTracking()
            .Where(c => c.IsVisible && c.Records.Any(r => r.Status == ContentStatus.Published))
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<int>> GetResearchYearsAsync(CancellationToken ct = default) =>
        await _db.Research.AsNoTracking()
            .Where(r => r.Status == ContentStatus.Published && r.PublicationDate != null)
            .Select(r => r.PublicationDate!.Value.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Certification>> GetCertificationsAsync(CancellationToken ct = default) =>
        await _db.Certifications.AsNoTracking()
            .Where(x => x.Status == ContentStatus.Published)
            .OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.IssueDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Achievement>> GetAchievementsAsync(CancellationToken ct = default) =>
        await _db.Achievements.AsNoTracking()
            .Where(x => x.IsVisible)
            .OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.AchievementDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken ct = default) =>
        await _db.Services.AsNoTracking()
            .Where(x => x.IsVisible)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Testimonial>> GetTestimonialsAsync(CancellationToken ct = default) =>
        await _db.Testimonials.AsNoTracking()
            .Where(x => x.Status == ContentStatus.Published)
            .OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.TestimonialDate)
            .Take(12)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SocialLink>> GetSocialLinksAsync(CancellationToken ct = default) =>
        await _db.SocialLinks.AsNoTracking()
            .Where(x => x.IsVisible)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id)
            .ToListAsync(ct);

    public async Task<int> GetPublishedProjectCountAsync(CancellationToken ct = default) =>
        await _db.Projects.CountAsync(p => p.Status == ContentStatus.Published, ct);
}
