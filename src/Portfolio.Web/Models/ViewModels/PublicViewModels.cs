using System.ComponentModel.DataAnnotations;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;

namespace Portfolio.Web.Models.ViewModels;

public record SeoModel(
    string? Title,
    string? Description,
    string? ImageUrl = null,
    string? CanonicalPath = null,
    bool NoIndex = false,
    string Type = "website");

public class HomeViewModel
{
    public SiteSettings Settings { get; init; } = new();
    public Profile Profile { get; init; } = new();
    public HeroSettings Hero { get; init; } = new();
    public IReadOnlyList<SocialLink> HeroSocialLinks { get; init; } = [];
    public IReadOnlyList<AboutSection> AboutSections { get; init; } = [];
    public IReadOnlyList<SkillCategory> SkillCategories { get; init; } = [];
    public IReadOnlyList<Project> Projects { get; init; } = [];
    public IReadOnlyList<Experience> Experience { get; init; } = [];
    public IReadOnlyList<Research> Research { get; init; } = [];
    public IReadOnlyList<Service> Services { get; init; } = [];
    public IReadOnlyList<Testimonial> Testimonials { get; init; } = [];
    public IReadOnlyList<Certification> Certifications { get; init; } = [];
}

public class AboutViewModel
{
    public SiteSettings Settings { get; init; } = new();
    public Profile Profile { get; init; } = new();
    public IReadOnlyList<AboutSection> Sections { get; init; } = [];
    public IReadOnlyList<Experience> Experience { get; init; } = [];
    public IReadOnlyList<Education> Education { get; init; } = [];
    public IReadOnlyList<Certification> Certifications { get; init; } = [];
    public IReadOnlyList<Achievement> Achievements { get; init; } = [];
    public IReadOnlyList<SkillCategory> SkillCategories { get; init; } = [];
}

public class SkillsViewModel
{
    public SiteSettings Settings { get; init; } = new();
    public Profile Profile { get; init; } = new();
    public IReadOnlyList<SkillCategory> Categories { get; init; } = [];
}

public class ProjectsIndexViewModel
{
    public SiteSettings Settings { get; init; } = new();
    public PagedList<Project> Projects { get; init; } = new();
    public IReadOnlyList<string> Categories { get; init; } = [];
    public string? Query { get; init; }
    public string? Category { get; init; }
    public string? Tech { get; init; }
}

public class ProjectDetailViewModel
{
    public SiteSettings Settings { get; init; } = new();
    public Project Project { get; init; } = new();
    public IReadOnlyList<Project> Related { get; init; } = [];
}

public class ResearchIndexViewModel
{
    public SiteSettings Settings { get; init; } = new();
    public PagedList<Research> Records { get; init; } = new();
    public IReadOnlyList<ResearchCategory> Categories { get; init; } = [];
    public IReadOnlyList<int> Years { get; init; } = [];
    public string? Query { get; init; }
    public string? Category { get; init; }
    public string? Tag { get; init; }
    public int? Year { get; init; }
}

public class ResearchDetailViewModel
{
    public SiteSettings Settings { get; init; } = new();
    public Research Research { get; init; } = new();
    public IReadOnlyList<Research> Related { get; init; } = [];

    public string GenerateBibTeX()
    {
        var year = Research.PublicationDate?.Year ?? (Research.StartDate?.Year ?? DateTime.UtcNow.Year);
        var primaryAuthor = Research.Authors.OrderBy(a => a.DisplayOrder).FirstOrDefault()?.Name ?? "Author";
        var firstWordOfLastName = primaryAuthor.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.ToLowerInvariant() ?? "author";
        var citationKey = $"{firstWordOfLastName}{year}_{Research.Slug.Replace('-', '_')}";
        var authorList = Research.Authors.Count > 0
            ? string.Join(" and ", Research.Authors.OrderBy(a => a.DisplayOrder).Select(a => a.Name))
            : primaryAuthor;
        var venue = !string.IsNullOrWhiteSpace(Research.Journal)
            ? Research.Journal
            : (!string.IsNullOrWhiteSpace(Research.PublicationVenue) ? Research.PublicationVenue : "Working Paper");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"@article{{{citationKey},");
        sb.AppendLine($"  title = {{{Research.Title}}},");
        sb.AppendLine($"  author = {{{authorList}}},");
        sb.AppendLine($"  journal = {{{venue}}},");
        sb.Append($"  year = {{{year}}}");
        if (!string.IsNullOrWhiteSpace(Research.Doi))
        {
            sb.AppendLine(",");
            sb.Append($"  doi = {{{Research.Doi}}}");
        }
        else if (!string.IsNullOrWhiteSpace(Research.ExternalUrl))
        {
            sb.AppendLine(",");
            sb.Append($"  url = {{{Research.ExternalUrl}}}");
        }
        sb.AppendLine();
        sb.Append('}');
        return sb.ToString();
    }

    public string GenerateApa()
    {
        var year = Research.PublicationDate?.Year ?? (Research.StartDate?.Year ?? DateTime.UtcNow.Year);
        var authors = Research.Authors.OrderBy(a => a.DisplayOrder).Select(a => a.Name).ToList();
        string authorStr;
        if (authors.Count == 0) authorStr = "Author";
        else if (authors.Count == 1) authorStr = authors[0];
        else if (authors.Count == 2) authorStr = $"{authors[0]} & {authors[1]}";
        else authorStr = $"{authors[0]} et al.";

        var venue = !string.IsNullOrWhiteSpace(Research.Journal)
            ? Research.Journal
            : (!string.IsNullOrWhiteSpace(Research.PublicationVenue) ? Research.PublicationVenue : null);

        var venuePart = venue is not null ? $" {venue}." : "";
        var doiPart = !string.IsNullOrWhiteSpace(Research.Doi)
            ? $" https://doi.org/{Research.Doi.Replace("https://doi.org/", "")}"
            : (!string.IsNullOrWhiteSpace(Research.ExternalUrl) ? $" {Research.ExternalUrl}" : "");

        return $"{authorStr} ({year}). {Research.Title}.{venuePart}{doiPart}".Trim();
    }
}

public class ServicesViewModel
{
    public SiteSettings Settings { get; init; } = new();
    public Profile Profile { get; init; } = new();
    public IReadOnlyList<Service> Services { get; init; } = [];
    public IReadOnlyList<Testimonial> Testimonials { get; init; } = [];
}

public class ContactPageViewModel
{
    public SiteSettings Settings { get; init; } = new();
    public Profile Profile { get; init; } = new();
    public IReadOnlyList<SocialLink> SocialLinks { get; init; } = [];
    public ContactFormModel Form { get; init; } = new();
    public bool FormAvailable => Settings.ShowContactForm;
}

public class ContactFormModel
{
    [Required]
    [StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Subject { get; set; }

    [Required]
    [StringLength(4000, MinimumLength = 10)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Honeypot. Must stay empty; bots that fill it are rejected silently.</summary>
    public string? Website { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Please confirm that you agree to be contacted.")]
    public bool Consent { get; set; }
}

public class ErrorViewModel
{
    public int StatusCode { get; init; }
    public string Title { get; init; } = "Something went wrong";
    public string Message { get; init; } = "The page could not be displayed.";
}
