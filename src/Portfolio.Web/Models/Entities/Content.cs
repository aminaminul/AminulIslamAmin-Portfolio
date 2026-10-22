using System.ComponentModel.DataAnnotations;

namespace Portfolio.Web.Models.Entities;

public class Certification : PublishableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? TitleBn { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string? IssuerUrl { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? CredentialId { get; set; }
    public string? VerificationUrl { get; set; }
    public int? ImageMediaId { get; set; }
    public MediaFile? Image { get; set; }
    public string? Description { get; set; }
    public string? DescriptionBn { get; set; }
}

public class Achievement : BaseEntity, IVisibleContent
{
    public string Title { get; set; } = string.Empty;
    public string? TitleBn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionBn { get; set; }
    public DateTime? AchievementDate { get; set; }
    public string? IssuingOrganization { get; set; }
    public int? ImageMediaId { get; set; }
    public MediaFile? Image { get; set; }
    public string? ExternalUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
}

public class Service : BaseEntity, IVisibleContent
{
    public string Title { get; set; } = string.Empty;
    public string? TitleBn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionBn { get; set; }
    public string? IconName { get; set; }
    /// <summary>Optional price the owner explicitly supplies; null means "not published".</summary>
    public decimal? StartingPrice { get; set; }
    public string? Currency { get; set; }
    public string? CtaLabel { get; set; }
    public string? CtaUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
}

public class Testimonial : PublishableEntity
{
    public string ClientName { get; set; } = string.Empty;
    public string? ClientRole { get; set; }
    public string? Organization { get; set; }
    public string Quote { get; set; } = string.Empty;
    public string? QuoteBn { get; set; }
    public int? AvatarMediaId { get; set; }
    public MediaFile? Avatar { get; set; }
    public DateTime? TestimonialDate { get; set; }
    public string? SourceUrl { get; set; }
}

public class SocialLink : BaseEntity, IVisibleContent
{
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? IconName { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool ShowInHero { get; set; } = true;
    public bool ShowInFooter { get; set; } = true;
}

public class NavigationItem : BaseEntity, IVisibleContent
{
    public string Label { get; set; } = string.Empty;
    public string? LabelBn { get; set; }
    public string Url { get; set; } = "/";
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool OpenInNewTab { get; set; }
    public bool ShowInFooter { get; set; }
    /// <summary>Key of the feature section this link points to (projects, research, ...).</summary>
    public string? SectionKey { get; set; }
}

public class MediaFile : BaseEntity
{
    /// <summary>Server-generated file name; never derived from user input.</summary>
    public string StorageName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public string Extension { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public MediaKind Kind { get; set; }
    public string? UploadedBy { get; set; }
}

public class ContactMessage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public bool IsArchived { get; set; }
    public string? IpHash { get; set; }
    public EmailDeliveryState EmailState { get; set; }
    public string? EmailError { get; set; }
}
