using Microsoft.AspNetCore.Mvc;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class SettingsController : AdminControllerBase
{
    private readonly ISiteSettingsService _settings;

    public SettingsController(Data.AppDbContext db, IMediaService media, ISiteSettingsService settings) : base(db, media)
    {
        _settings = settings;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var settings = await _settings.GetTrackedAsync(ct);
        await LoadImageChoicesAsync(ct, "Logo");
        return View(SiteSettingsFormModel.From(settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SiteSettingsFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadImageChoicesAsync(ct, "Logo");
            return View(form);
        }

        var settings = await _settings.GetTrackedAsync(ct);
        try
        {
            form.LogoMediaId = await KeepOrReplaceAsync(form.LogoFile, form.LogoMediaId, MediaKind.Image, "Site logo", ct);
        }
        catch (MediaValidationException)
        {
            await LoadImageChoicesAsync(ct, "Logo");
            return View(form);
        }

        form.ApplyTo(settings);
        await Db.SaveChangesAsync(ct);
        _settings.InvalidateCache();
        NotifySuccess("Website settings saved.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Contact(CancellationToken ct)
    {
        var settings = await _settings.GetTrackedAsync(ct);
        return View(ContactSettingsFormModel.From(settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactSettingsFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(form);

        var settings = await _settings.GetTrackedAsync(ct);
        form.ApplyTo(settings);
        await Db.SaveChangesAsync(ct);
        _settings.InvalidateCache();
        NotifySuccess("Contact information saved.");
        return RedirectToAction(nameof(Contact));
    }

    [HttpGet]
    public async Task<IActionResult> Seo(CancellationToken ct)
    {
        var settings = await _settings.GetTrackedAsync(ct);
        await LoadImageChoicesAsync(ct, "OgImage");
        return View(SeoSettingsFormModel.From(settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Seo(SeoSettingsFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadImageChoicesAsync(ct, "OgImage");
            return View(form);
        }

        var settings = await _settings.GetTrackedAsync(ct);
        try
        {
            form.OgImageMediaId = await KeepOrReplaceAsync(form.OgImageFile, form.OgImageMediaId, MediaKind.Image,
                "Social sharing image", ct);
        }
        catch (MediaValidationException)
        {
            await LoadImageChoicesAsync(ct, "OgImage");
            return View(form);
        }

        form.ApplyTo(settings);
        await Db.SaveChangesAsync(ct);
        _settings.InvalidateCache();
        NotifySuccess("SEO settings saved.");
        return RedirectToAction(nameof(Seo));
    }

    [HttpGet]
    public async Task<IActionResult> Footer(CancellationToken ct)
    {
        var settings = await _settings.GetTrackedAsync(ct);
        return View(FooterSettingsFormModel.From(settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Footer(FooterSettingsFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(form);

        var settings = await _settings.GetTrackedAsync(ct);
        form.ApplyTo(settings);
        await Db.SaveChangesAsync(ct);
        _settings.InvalidateCache();
        NotifySuccess("Footer saved.");
        return RedirectToAction(nameof(Footer));
    }
}
