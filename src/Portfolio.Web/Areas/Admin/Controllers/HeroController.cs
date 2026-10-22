using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class HeroController : AdminControllerBase
{
    public HeroController(Data.AppDbContext db, IMediaService media) : base(db, media) { }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var hero = await Db.HeroSettings.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(ct) ?? new HeroSettings();
        await LoadImageChoicesAsync(ct, "Image");
        return View(HeroFormModel.From(hero));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(HeroFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadImageChoicesAsync(ct, "Image");
            return View(form);
        }

        var hero = await Db.HeroSettings.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (hero is null)
        {
            hero = new HeroSettings();
            Db.HeroSettings.Add(hero);
        }

        try
        {
            form.ImageMediaId = await KeepOrReplaceAsync(form.ImageFile, form.ImageMediaId, MediaKind.Image, null, ct);
        }
        catch (MediaValidationException)
        {
            await LoadImageChoicesAsync(ct, "Image");
            return View(form);
        }

        form.ApplyTo(hero);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Hero section saved.");
        return RedirectToAction(nameof(Index));
    }
}
