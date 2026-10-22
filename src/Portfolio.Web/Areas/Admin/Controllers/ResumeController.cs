using Microsoft.AspNetCore.Mvc;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class ResumeController : AdminControllerBase
{
    private readonly ISiteSettingsService _settings;

    public ResumeController(Data.AppDbContext db, IMediaService media, ISiteSettingsService settings) : base(db, media)
    {
        _settings = settings;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var settings = await _settings.GetTrackedAsync(ct);
        await LoadChoicesAsync("Resume", MediaKind.Document, ct);
        return View(ResumeFormModel.From(settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Index(ResumeFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadChoicesAsync("Resume", MediaKind.Document, ct);
            return View(form);
        }

        var settings = await _settings.GetTrackedAsync(ct);
        try
        {
            form.ResumeMediaId = await KeepOrReplaceAsync(form.ResumeFile, form.ResumeMediaId, MediaKind.Document,
                "Resume / CV", ct);
        }
        catch (MediaValidationException)
        {
            await LoadChoicesAsync("Resume", MediaKind.Document, ct);
            return View(form);
        }

        form.ApplyTo(settings);
        await Db.SaveChangesAsync(ct);
        _settings.InvalidateCache();
        NotifySuccess("Resume settings saved.");
        return RedirectToAction(nameof(Index));
    }
}
