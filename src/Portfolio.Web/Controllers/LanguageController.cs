using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Portfolio.Web.Controllers;

public class LanguageController : Controller
{
    private static readonly string[] Supported = ["en", "bn"];

    [HttpGet("/language/set")]
    public IActionResult Set(string culture, string? returnUrl)
    {
        if (!Supported.Contains(culture, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Unsupported culture.");

        var requestCulture = new RequestCulture(culture, culture);
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(requestCulture),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }
}
