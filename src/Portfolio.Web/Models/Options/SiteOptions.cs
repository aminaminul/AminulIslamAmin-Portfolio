namespace Portfolio.Web.Models.Options;

/// <summary>Public base URL used for canonical links and the sitemap. Falls back to the request host.</summary>
public class SiteOptions
{
    public const string SectionName = "Site";
    public string? PublicBaseUrl { get; set; }
}
