namespace Portfolio.Web.Infrastructure;

/// <summary>Allowlist of icons available to content editors (rendered by Views/Shared/_Icon.cshtml).</summary>
public static class IconCatalog
{
    public static readonly string[] Names =
    [
        "code", "server", "layout", "database", "wrench", "brain", "cloud", "shield",
        "terminal", "git-branch", "box", "cpu", "layers", "globe", "briefcase",
        "graduation-cap", "award", "file-text", "mail", "sparkles"
    ];

    public static readonly string[] SocialNames =
    [
        "github", "linkedin", "twitter", "facebook", "instagram", "youtube",
        "dribbble", "globe", "mail", "scholar"
    ];

    public static bool IsValid(string? name) => name is not null && Names.Contains(name);
    public static bool IsSocial(string? name) => name is not null && SocialNames.Contains(name);
}
