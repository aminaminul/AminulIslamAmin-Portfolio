using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.ViewModels.Admin;
using Portfolio.Web.Services;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class ProfileController : AdminControllerBase
{
    public ProfileController(Data.AppDbContext db, IMediaService media) : base(db, media) { }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var profile = await Db.Profiles.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(ct) ?? new Profile();
        await LoadImageChoicesAsync(ct, "Photo");
        return View(ProfileFormModel.From(profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadImageChoicesAsync(ct, "Photo");
            return View(form);
        }

        var profile = await Db.Profiles.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (profile is null)
        {
            profile = new Profile();
            Db.Profiles.Add(profile);
        }

        try
        {
            form.PhotoMediaId = await KeepOrReplaceAsync(form.PhotoFile, form.PhotoMediaId, MediaKind.Image, form.PhotoAlt, ct);
        }
        catch (MediaValidationException)
        {
            await LoadImageChoicesAsync(ct, "Photo");
            return View(form);
        }

        form.ApplyTo(profile);
        await Db.SaveChangesAsync(ct);
        NotifySuccess("Profile saved. The public website now uses these details.");
        return RedirectToAction(nameof(Index));
    }
}
