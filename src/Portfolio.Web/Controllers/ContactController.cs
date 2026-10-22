using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Web.Models.ViewModels;
using Portfolio.Web.Services;

namespace Portfolio.Web.Controllers;

public class ContactController : Controller
{
    private readonly ISiteSettingsService _settings;
    private readonly IPortfolioQueryService _query;
    private readonly IContactService _contact;
    private readonly ILogger<ContactController> _logger;

    public ContactController(ISiteSettingsService settings, IPortfolioQueryService query, IContactService contact, ILogger<ContactController> logger)
    {
        _settings = settings;
        _query = query;
        _contact = contact;
        _logger = logger;
    }

    [HttpGet("/contact")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var model = await BuildAsync(new ContactFormModel(), ct);
        ViewData["Seo"] = new SeoModel("Contact", model.Settings.ContactIntro, CanonicalPath: "/contact");
        return View(model);
    }

    [HttpPost("/contact")]
    [EnableRateLimiting("contact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContactFormModel form, CancellationToken ct)
    {
        // Honeypot: real visitors never see or fill this field.
        if (!string.IsNullOrWhiteSpace(form.Website))
        {
            _logger.LogWarning("Contact honeypot triggered from {Ip}", HttpContext.Connection.RemoteIpAddress);
            TempData["ContactSuccess"] = "Thank you for your message.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildAsync(form, ct));
        }

        var result = await _contact.SubmitAsync(
            form.Name, form.Email, form.Subject, form.Message,
            HttpContext.Connection.RemoteIpAddress?.ToString(), ct);

        if (!result.Saved)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(await BuildAsync(form, ct));
        }

        TempData["ContactSuccess"] = result.Message;
        TempData["ContactWarning"] = !result.EmailSent;
        return RedirectToAction(nameof(Index));
    }

    private async Task<ContactPageViewModel> BuildAsync(ContactFormModel form, CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        return new ContactPageViewModel
        {
            Settings = settings,
            Profile = await _query.GetProfileAsync(ct),
            SocialLinks = await _query.GetSocialLinksAsync(ct),
            Form = form
        };
    }
}
