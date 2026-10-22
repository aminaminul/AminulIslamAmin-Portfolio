using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;
using Portfolio.Web.Models.Entities;

namespace Portfolio.Web.Services;

public record SitemapEntry(string Path, DateTime LastModified, string ChangeFrequency, decimal Priority);

public interface ISitemapService
{
    Task<IReadOnlyList<SitemapEntry>> GetEntriesAsync(CancellationToken ct = default);
    Task<string> BuildXmlAsync(string baseUrl, CancellationToken ct = default);
}

public class SitemapService : ISitemapService
{
    private readonly AppDbContext _db;

    public SitemapService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SitemapEntry>> GetEntriesAsync(CancellationToken ct = default)
    {
        var entries = new List<SitemapEntry>
        {
            new("/", DateTime.UtcNow, "weekly", 1.0m),
            new("/about", DateTime.UtcNow, "monthly", 0.8m),
            new("/skills", DateTime.UtcNow, "monthly", 0.7m),
            new("/projects", DateTime.UtcNow, "weekly", 0.9m),
            new("/research", DateTime.UtcNow, "weekly", 0.9m),
            new("/services", DateTime.UtcNow, "monthly", 0.7m),
            new("/contact", DateTime.UtcNow, "yearly", 0.6m)
        };

        var projects = await _db.Projects.AsNoTracking()
            .Where(p => p.Status == ContentStatus.Published)
            .Select(p => new { p.Slug, p.UpdatedAt })
            .ToListAsync(ct);

        entries.AddRange(projects.Select(p =>
            new SitemapEntry($"/projects/{Uri.EscapeDataString(p.Slug)}", p.UpdatedAt, "monthly", 0.8m)));

        var research = await _db.Research.AsNoTracking()
            .Where(r => r.Status == ContentStatus.Published)
            .Select(r => new { r.Slug, r.UpdatedAt })
            .ToListAsync(ct);

        entries.AddRange(research.Select(r =>
            new SitemapEntry($"/research/{Uri.EscapeDataString(r.Slug)}", r.UpdatedAt, "monthly", 0.8m)));

        return entries;
    }

    public async Task<string> BuildXmlAsync(string baseUrl, CancellationToken ct = default)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var entries = await GetEntriesAsync(ct);
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "urlset",
                entries.Select(e => new XElement(ns + "url",
                    new XElement(ns + "loc", baseUrl.TrimEnd('/') + e.Path),
                    new XElement(ns + "lastmod", e.LastModified.ToString("yyyy-MM-dd")),
                    new XElement(ns + "changefreq", e.ChangeFrequency),
                    new XElement(ns + "priority", e.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture))))));

        return document.ToString(SaveOptions.DisableFormatting);
    }
}
