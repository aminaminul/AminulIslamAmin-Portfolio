using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class DashboardController : AdminControllerBase
{
    private readonly ISiteSettingsService _settings;

    public DashboardController(Data.AppDbContext db, IMediaService media, ISiteSettingsService settings) : base(db, media)
    {
        _settings = settings;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var profile = await Db.Profiles.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(ct) ?? new Profile();

        var recent = new List<RecentActivity>();
        recent.AddRange(await Db.Projects.AsNoTracking()
            .OrderByDescending(p => p.UpdatedAt).Take(4)
            .Select(p => new RecentActivity(p.Title, "Project", p.UpdatedAt, "/Admin/Projects/Edit/" + p.Id))
            .ToListAsync(ct));
        recent.AddRange(await Db.Research.AsNoTracking()
            .OrderByDescending(r => r.UpdatedAt).Take(4)
            .Select(r => new RecentActivity(r.Title, "Research", r.UpdatedAt, "/Admin/Research/Edit/" + r.Id))
            .ToListAsync(ct));
        recent.AddRange(await Db.ContactMessages.AsNoTracking()
            .OrderByDescending(m => m.CreatedAt).Take(3)
            .Select(m => new RecentActivity("Message from " + m.Name, "Message", m.CreatedAt, "/Admin/Messages/Details/" + m.Id))
            .ToListAsync(ct));

        // Group counts by status to reduce round trips
        var projectCounts = await Db.Projects.AsNoTracking()
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var totalProjects = projectCounts.Sum(x => x.Count);
        var publishedProjects = projectCounts.FirstOrDefault(x => x.Status == ContentStatus.Published)?.Count ?? 0;
        var draftProjects = projectCounts.FirstOrDefault(x => x.Status == ContentStatus.Draft)?.Count ?? 0;

        var researchCounts = await Db.Research.AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var totalResearch = researchCounts.Sum(x => x.Count);
        var publishedResearch = researchCounts.FirstOrDefault(x => x.Status == ContentStatus.Published)?.Count ?? 0;
        var draftResearch = researchCounts.FirstOrDefault(x => x.Status == ContentStatus.Draft)?.Count ?? 0;

        var testimonialCounts = await Db.Testimonials.AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var publishedTestimonials = testimonialCounts.FirstOrDefault(x => x.Status == ContentStatus.Published)?.Count ?? 0;
        var pendingTestimonials = testimonialCounts.FirstOrDefault(x => x.Status == ContentStatus.Draft)?.Count ?? 0;

        var messageCounts = await Db.ContactMessages.AsNoTracking()
            .GroupBy(m => new { m.IsRead, m.IsArchived })
            .Select(g => new { g.Key.IsRead, g.Key.IsArchived, Count = g.Count() })
            .ToListAsync(ct);
        var totalMessages = messageCounts.Sum(x => x.Count);
        var unreadMessages = messageCounts.Where(x => !x.IsRead && !x.IsArchived).Sum(x => x.Count);

        var model = new DashboardViewModel
        {
            Settings = await _settings.GetAsync(ct),
            Profile = profile,
            TotalProjects = totalProjects,
            PublishedProjects = publishedProjects,
            DraftProjects = draftProjects,
            TotalResearch = totalResearch,
            PublishedResearch = publishedResearch,
            DraftResearch = draftResearch,
            TotalSkills = await Db.Skills.CountAsync(ct),
            TotalCertifications = await Db.Certifications.CountAsync(ct),
            TotalExperience = await Db.Experiences.CountAsync(ct),
            TotalEducation = await Db.Educations.CountAsync(ct),
            PendingTestimonials = pendingTestimonials,
            PublishedTestimonials = publishedTestimonials,
            TotalServices = await Db.Services.CountAsync(ct),
            MediaFiles = await Db.MediaFiles.CountAsync(ct),
            UnreadMessages = unreadMessages,
            TotalMessages = totalMessages,
            PublishedAboutSections = await Db.AboutSections.CountAsync(a => a.Status == ContentStatus.Published && a.IsVisible, ct),
            Recent = recent.OrderByDescending(r => r.When).Take(8).ToList()
        };

        return View(model);
    }
}
