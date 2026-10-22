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

        var model = new DashboardViewModel
        {
            Settings = await _settings.GetAsync(ct),
            Profile = profile,
            TotalProjects = await Db.Projects.CountAsync(ct),
            PublishedProjects = await Db.Projects.CountAsync(p => p.Status == ContentStatus.Published, ct),
            DraftProjects = await Db.Projects.CountAsync(p => p.Status == ContentStatus.Draft, ct),
            TotalResearch = await Db.Research.CountAsync(ct),
            PublishedResearch = await Db.Research.CountAsync(r => r.Status == ContentStatus.Published, ct),
            DraftResearch = await Db.Research.CountAsync(r => r.Status == ContentStatus.Draft, ct),
            TotalSkills = await Db.Skills.CountAsync(ct),
            TotalCertifications = await Db.Certifications.CountAsync(ct),
            TotalExperience = await Db.Experiences.CountAsync(ct),
            TotalEducation = await Db.Educations.CountAsync(ct),
            PendingTestimonials = await Db.Testimonials.CountAsync(t => t.Status == ContentStatus.Draft, ct),
            PublishedTestimonials = await Db.Testimonials.CountAsync(t => t.Status == ContentStatus.Published, ct),
            TotalServices = await Db.Services.CountAsync(ct),
            MediaFiles = await Db.MediaFiles.CountAsync(ct),
            UnreadMessages = await Db.ContactMessages.CountAsync(m => !m.IsRead && !m.IsArchived, ct),
            TotalMessages = await Db.ContactMessages.CountAsync(ct),
            PublishedAboutSections = await Db.AboutSections.CountAsync(a => a.Status == ContentStatus.Published && a.IsVisible, ct),
            Recent = recent.OrderByDescending(r => r.When).Take(8).ToList()
        };

        return View(model);
    }
}
