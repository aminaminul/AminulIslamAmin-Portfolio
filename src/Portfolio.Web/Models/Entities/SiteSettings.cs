using System.ComponentModel.DataAnnotations;

namespace Portfolio.Web.Models.Entities;

/// <summary>Single-row table holding website identity, contact details, resume and SEO defaults.</summary>
public class SiteSettings : BaseEntity
{
    public string SiteName { get; set; } = "Portfolio";
    public string? SiteNameBn { get; set; }
    public string? Tagline { get; set; }
    public string? TaglineBn { get; set; }
    public string? LogoText { get; set; }
    public int? LogoMediaId { get; set; }
    public MediaFile? Logo { get; set; }

    public string? FooterDescription { get; set; }
    public string? FooterDescriptionBn { get; set; }
    public string? CopyrightText { get; set; }

    public string? AvailabilityText { get; set; }
    public string? AvailabilityTextBn { get; set; }
    public bool IsAvailable { get; set; } = true;

    public string? ContactIntro { get; set; }
    public string? ContactIntroBn { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactLocation { get; set; }
    public bool ShowContactForm { get; set; } = true;
    public string? ContactFormRecipient { get; set; }

    public int? ResumeMediaId { get; set; }
    public MediaFile? Resume { get; set; }
    public string? ResumeHeadline { get; set; }
    public string? ResumeHeadlineBn { get; set; }
    public string? ResumeButtonLabel { get; set; } = "Download CV";
    public string? ResumeButtonLabelBn { get; set; }
    public bool ShowResumeSection { get; set; } = true;

    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoKeywords { get; set; }
    public int? OgImageMediaId { get; set; }
    public MediaFile? OgImage { get; set; }
    public string DefaultTheme { get; set; } = "system";
    public string? AccentColor { get; set; }
    public string? RobotsTxtExtra { get; set; }
}

/// <summary>Single-row owner profile. All public identity data lives here.</summary>
public class Profile : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ProfessionalTitle { get; set; }
    public string? ProfessionalTitleBn { get; set; }
    public string? ShortIntro { get; set; }
    public string? ShortIntroBn { get; set; }
    public string? Location { get; set; }
    public int? PhotoMediaId { get; set; }
    public MediaFile? Photo { get; set; }
    public string? PhotoAlt { get; set; }
}

/// <summary>Single-row hero configuration, including CTA labels and visibility toggles.</summary>
public class HeroSettings : BaseEntity
{
    public string? BadgeText { get; set; }
    public string? BadgeTextBn { get; set; }
    public string? Headline { get; set; }
    public string? HeadlineBn { get; set; }
    public string? Subheadline { get; set; }
    public string? SubheadlineBn { get; set; }
    public int? ImageMediaId { get; set; }
    public MediaFile? Image { get; set; }

    public string PrimaryCtaLabel { get; set; } = "View projects";
    public string? PrimaryCtaLabelBn { get; set; }
    public string PrimaryCtaUrl { get; set; } = "/projects";
    public bool ShowPrimaryCta { get; set; } = true;

    public string SecondaryCtaLabel { get; set; } = "Get in touch";
    public string? SecondaryCtaLabelBn { get; set; }
    public string SecondaryCtaUrl { get; set; } = "/contact";
    public bool ShowSecondaryCta { get; set; } = true;

    public bool ShowResumeButton { get; set; } = true;
    public bool ShowSocialLinks { get; set; } = true;
    public bool ShowPhoto { get; set; } = true;
    public string ImagePosition { get; set; } = "right";
}

public class AboutSection : PublishableEntity, IVisibleContent
{
    public string Title { get; set; } = string.Empty;
    public string? TitleBn { get; set; }
    /// <summary>Restricted Markdown; rendered with raw HTML disabled.</summary>
    public string? Body { get; set; }
    public string? BodyBn { get; set; }
    public int? ImageMediaId { get; set; }
    public MediaFile? Image { get; set; }
    public bool IsVisible { get; set; } = true;
}
