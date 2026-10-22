using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Services;

namespace Portfolio.Web.Controllers;

public class MediaController : Controller
{
    private readonly AppDbContext _db;
    private readonly IMediaService _media;
    private readonly ISiteSettingsService _settings;

    public MediaController(AppDbContext db, IMediaService media, ISiteSettingsService settings)
    {
        _db = db;
        _media = media;
        _settings = settings;
    }

    [HttpGet("/media/{id:int}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Index(int id, bool download = false, CancellationToken ct = default)
    {
        var media = await _db.MediaFiles.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
        if (media is null) return NotFound();

        var resolved = _media.Resolve(media);
        if (resolved is null)
        {
            // The record exists but the file is missing: never claim success with broken media.
            return NotFound();
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        var stream = new FileStream(resolved.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        var downloadName = SafeDownloadName(resolved.DownloadName);

        if (download || resolved.Kind == MediaKind.Document)
            return File(stream, resolved.ContentType, downloadName, enableRangeProcessing: true);

        return File(stream, resolved.ContentType, enableRangeProcessing: true);
    }

    [HttpGet("/cv")]
    public async Task<IActionResult> Cv(CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        if (!settings.ShowResumeSection || settings.ResumeMediaId is null) return NotFound();
        return RedirectToAction(nameof(Index), new { id = settings.ResumeMediaId, download = true });
    }

    private static string SafeDownloadName(string name)
    {
        var cleaned = Path.GetFileName(name);
        return string.IsNullOrWhiteSpace(cleaned) ? "download" : cleaned;
    }
}
