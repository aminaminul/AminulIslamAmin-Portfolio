using System.Text;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Web.Services;

namespace Portfolio.Web.Controllers;

public class SitemapController : Controller
{
    private readonly ISitemapService _sitemap;
    private readonly ISiteSettingsService _settings;
    private readonly IConfiguration _configuration;

    public SitemapController(ISitemapService sitemap, ISiteSettingsService settings, IConfiguration configuration)
    {
        _sitemap = sitemap;
        _settings = settings;
        _configuration = configuration;
    }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Xml(CancellationToken ct)
    {
        var xml = await _sitemap.BuildXmlAsync(BaseUrl(), ct);
        return Content(xml, "application/xml", Encoding.UTF8);
    }

    [HttpGet("/robots.txt")]
    public async Task<IActionResult> Robots(CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        var builder = new StringBuilder();
        builder.AppendLine("User-agent: *");
        builder.AppendLine("Allow: /");
        builder.AppendLine("Disallow: /Admin");
        builder.AppendLine($"Sitemap: {BaseUrl().TrimEnd('/')}/sitemap.xml");
        if (!string.IsNullOrWhiteSpace(settings.RobotsTxtExtra))
        {
            builder.AppendLine();
            builder.AppendLine(settings.RobotsTxtExtra.Trim());
        }
        return Content(builder.ToString(), "text/plain", Encoding.UTF8);
    }

    private string BaseUrl()
    {
        var configured = _configuration["Site:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured.TrimEnd('/');
        return $"{Request.Scheme}://{Request.Host}";
    }
}
