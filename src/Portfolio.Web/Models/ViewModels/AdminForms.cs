using System.ComponentModel.DataAnnotations;
using Portfolio.Web.Infrastructure;
using Portfolio.Web.Models.Entities;

namespace Portfolio.Web.Models.ViewModels.Admin;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 12)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfileFormModel
{
    [Required]
    [StringLength(160)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(120)]
    public string? DisplayName { get; set; }

    [StringLength(160)]
    public string? ProfessionalTitle { get; set; }

    [StringLength(200)]
    public string? ProfessionalTitleBn { get; set; }

    [StringLength(500)]
    public string? ShortIntro { get; set; }

    [StringLength(600)]
    public string? ShortIntroBn { get; set; }

    [StringLength(160)]
    public string? Location { get; set; }

    public int? PhotoMediaId { get; set; }
    public IFormFile? PhotoFile { get; set; }

    [StringLength(250)]
    public string? PhotoAlt { get; set; }

    public static ProfileFormModel From(Profile p) => new()
    {
        FullName = p.FullName,
        DisplayName = p.DisplayName,
        ProfessionalTitle = p.ProfessionalTitle,
        ProfessionalTitleBn = p.ProfessionalTitleBn,
        ShortIntro = p.ShortIntro,
        ShortIntroBn = p.ShortIntroBn,
        Location = p.Location,
        PhotoMediaId = p.PhotoMediaId,
        PhotoAlt = p.PhotoAlt
    };

    public void ApplyTo(Profile p)
    {
        p.FullName = FullName.Trim();
        p.DisplayName = Clean(DisplayName);
        p.ProfessionalTitle = Clean(ProfessionalTitle);
        p.ProfessionalTitleBn = Clean(ProfessionalTitleBn);
        p.ShortIntro = Clean(ShortIntro);
        p.ShortIntroBn = Clean(ShortIntroBn);
        p.Location = Clean(Location);
        p.PhotoMediaId = PhotoMediaId;
        p.PhotoAlt = Clean(PhotoAlt);
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public class HeroFormModel
{
    [StringLength(80)] public string? BadgeText { get; set; }
    [StringLength(100)] public string? BadgeTextBn { get; set; }
    [StringLength(200)] public string? Headline { get; set; }
    [StringLength(250)] public string? HeadlineBn { get; set; }
    [StringLength(600)] public string? Subheadline { get; set; }
    [StringLength(700)] public string? SubheadlineBn { get; set; }
    public int? ImageMediaId { get; set; }
    public IFormFile? ImageFile { get; set; }

    [StringLength(60)] public string PrimaryCtaLabel { get; set; } = "View projects";
    [StringLength(80)] public string? PrimaryCtaLabelBn { get; set; }
    [StringLength(300)] public string PrimaryCtaUrl { get; set; } = "/projects";
    public bool ShowPrimaryCta { get; set; } = true;

    [StringLength(60)] public string SecondaryCtaLabel { get; set; } = "Get in touch";
    [StringLength(80)] public string? SecondaryCtaLabelBn { get; set; }
    [StringLength(300)] public string SecondaryCtaUrl { get; set; } = "/contact";
    public bool ShowSecondaryCta { get; set; } = true;

    public bool ShowResumeButton { get; set; } = true;
    public bool ShowSocialLinks { get; set; } = true;
    public bool ShowPhoto { get; set; } = true;

    [RegularExpression("^(left|right)$")]
    public string ImagePosition { get; set; } = "right";

    public static HeroFormModel From(HeroSettings h) => new()
    {
        BadgeText = h.BadgeText,
        BadgeTextBn = h.BadgeTextBn,
        Headline = h.Headline,
        HeadlineBn = h.HeadlineBn,
        Subheadline = h.Subheadline,
        SubheadlineBn = h.SubheadlineBn,
        ImageMediaId = h.ImageMediaId,
        PrimaryCtaLabel = h.PrimaryCtaLabel,
        PrimaryCtaLabelBn = h.PrimaryCtaLabelBn,
        PrimaryCtaUrl = h.PrimaryCtaUrl,
        ShowPrimaryCta = h.ShowPrimaryCta,
        SecondaryCtaLabel = h.SecondaryCtaLabel,
        SecondaryCtaLabelBn = h.SecondaryCtaLabelBn,
        SecondaryCtaUrl = h.SecondaryCtaUrl,
        ShowSecondaryCta = h.ShowSecondaryCta,
        ShowResumeButton = h.ShowResumeButton,
        ShowSocialLinks = h.ShowSocialLinks,
        ShowPhoto = h.ShowPhoto,
        ImagePosition = h.ImagePosition
    };

    public void ApplyTo(HeroSettings h)
    {
        h.BadgeText = Clean(BadgeText);
        h.BadgeTextBn = Clean(BadgeTextBn);
        h.Headline = Clean(Headline);
        h.HeadlineBn = Clean(HeadlineBn);
        h.Subheadline = Clean(Subheadline);
        h.SubheadlineBn = Clean(SubheadlineBn);
        h.ImageMediaId = ImageMediaId;
        h.PrimaryCtaLabel = PrimaryCtaLabel.Trim();
        h.PrimaryCtaLabelBn = Clean(PrimaryCtaLabelBn);
        h.PrimaryCtaUrl = PrimaryCtaUrl.Trim();
        h.ShowPrimaryCta = ShowPrimaryCta;
        h.SecondaryCtaLabel = SecondaryCtaLabel.Trim();
        h.SecondaryCtaLabelBn = Clean(SecondaryCtaLabelBn);
        h.SecondaryCtaUrl = SecondaryCtaUrl.Trim();
        h.ShowSecondaryCta = ShowSecondaryCta;
        h.ShowResumeButton = ShowResumeButton;
        h.ShowSocialLinks = ShowSocialLinks;
        h.ShowPhoto = ShowPhoto;
        h.ImagePosition = ImagePosition;
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public class SiteSettingsFormModel
{
    [Required]
    [StringLength(120)]
    public string SiteName { get; set; } = string.Empty;

    [StringLength(120)] public string? SiteNameBn { get; set; }
    [StringLength(200)] public string? Tagline { get; set; }
    [StringLength(200)] public string? TaglineBn { get; set; }
    [StringLength(60)] public string? LogoText { get; set; }
    public int? LogoMediaId { get; set; }
    public IFormFile? LogoFile { get; set; }
    public bool IsAvailable { get; set; }
    [StringLength(160)] public string? AvailabilityText { get; set; }
    [StringLength(160)] public string? AvailabilityTextBn { get; set; }
    [RegularExpression("^(system|light|dark)$")]
    public string DefaultTheme { get; set; } = "system";
    [RegularExpression("^$|^#[0-9a-fA-F]{6}$")]
    public string? AccentColor { get; set; }

    public static SiteSettingsFormModel From(SiteSettings s) => new()
    {
        SiteName = s.SiteName,
        SiteNameBn = s.SiteNameBn,
        Tagline = s.Tagline,
        TaglineBn = s.TaglineBn,
        LogoText = s.LogoText,
        LogoMediaId = s.LogoMediaId,
        IsAvailable = s.IsAvailable,
        AvailabilityText = s.AvailabilityText,
        AvailabilityTextBn = s.AvailabilityTextBn,
        DefaultTheme = s.DefaultTheme,
        AccentColor = s.AccentColor
    };

    public void ApplyTo(SiteSettings s)
    {
        s.SiteName = SiteName.Trim();
        s.SiteNameBn = Clean(SiteNameBn);
        s.Tagline = Clean(Tagline);
        s.TaglineBn = Clean(TaglineBn);
        s.LogoText = Clean(LogoText);
        s.LogoMediaId = LogoMediaId;
        s.IsAvailable = IsAvailable;
        s.AvailabilityText = Clean(AvailabilityText);
        s.AvailabilityTextBn = Clean(AvailabilityTextBn);
        s.DefaultTheme = DefaultTheme;
        s.AccentColor = Clean(AccentColor);
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public class ContactSettingsFormModel
{
    [EmailAddress]
    [StringLength(200)]
    public string? ContactEmail { get; set; }

    [StringLength(40)]
    public string? ContactPhone { get; set; }

    [StringLength(160)]
    public string? ContactLocation { get; set; }

    [StringLength(1000)]
    public string? ContactIntro { get; set; }

    [StringLength(1000)]
    public string? ContactIntroBn { get; set; }

    public bool ShowContactForm { get; set; }

    [EmailAddress]
    [StringLength(200)]
    public string? ContactFormRecipient { get; set; }

    public static ContactSettingsFormModel From(SiteSettings s) => new()
    {
        ContactEmail = s.ContactEmail,
        ContactPhone = s.ContactPhone,
        ContactLocation = s.ContactLocation,
        ContactIntro = s.ContactIntro,
        ContactIntroBn = s.ContactIntroBn,
        ShowContactForm = s.ShowContactForm,
        ContactFormRecipient = s.ContactFormRecipient
    };

    public void ApplyTo(SiteSettings s)
    {
        s.ContactEmail = Clean(ContactEmail);
        s.ContactPhone = Clean(ContactPhone);
        s.ContactLocation = Clean(ContactLocation);
        s.ContactIntro = Clean(ContactIntro);
        s.ContactIntroBn = Clean(ContactIntroBn);
        s.ShowContactForm = ShowContactForm;
        s.ContactFormRecipient = Clean(ContactFormRecipient);
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public class SeoSettingsFormModel
{
    [StringLength(200)] public string? SeoTitle { get; set; }
    [StringLength(400)] public string? SeoDescription { get; set; }
    [StringLength(400)] public string? SeoKeywords { get; set; }
    public int? OgImageMediaId { get; set; }
    public IFormFile? OgImageFile { get; set; }

    [StringLength(1000)]
    public string? RobotsTxtExtra { get; set; }

    public static SeoSettingsFormModel From(SiteSettings s) => new()
    {
        SeoTitle = s.SeoTitle,
        SeoDescription = s.SeoDescription,
        SeoKeywords = s.SeoKeywords,
        OgImageMediaId = s.OgImageMediaId,
        RobotsTxtExtra = s.RobotsTxtExtra
    };

    public void ApplyTo(SiteSettings s)
    {
        s.SeoTitle = Clean(SeoTitle);
        s.SeoDescription = Clean(SeoDescription);
        s.SeoKeywords = Clean(SeoKeywords);
        s.OgImageMediaId = OgImageMediaId;
        s.RobotsTxtExtra = Clean(RobotsTxtExtra);
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public class FooterSettingsFormModel
{
    [StringLength(600)] public string? FooterDescription { get; set; }
    [StringLength(600)] public string? FooterDescriptionBn { get; set; }
    [StringLength(200)] public string? CopyrightText { get; set; }

    public static FooterSettingsFormModel From(SiteSettings s) => new()
    {
        FooterDescription = s.FooterDescription,
        FooterDescriptionBn = s.FooterDescriptionBn,
        CopyrightText = s.CopyrightText
    };

    public void ApplyTo(SiteSettings s)
    {
        s.FooterDescription = string.IsNullOrWhiteSpace(FooterDescription) ? null : FooterDescription.Trim();
        s.FooterDescriptionBn = string.IsNullOrWhiteSpace(FooterDescriptionBn) ? null : FooterDescriptionBn.Trim();
        s.CopyrightText = string.IsNullOrWhiteSpace(CopyrightText) ? null : CopyrightText.Trim();
    }
}

public class ResumeFormModel
{
    public int? ResumeMediaId { get; set; }
    public IFormFile? ResumeFile { get; set; }
    [StringLength(400)] public string? ResumeHeadline { get; set; }
    [StringLength(400)] public string? ResumeHeadlineBn { get; set; }
    [StringLength(80)] public string? ResumeButtonLabel { get; set; }
    [StringLength(80)] public string? ResumeButtonLabelBn { get; set; }
    public bool ShowResumeSection { get; set; }

    public static ResumeFormModel From(SiteSettings s) => new()
    {
        ResumeMediaId = s.ResumeMediaId,
        ResumeHeadline = s.ResumeHeadline,
        ResumeHeadlineBn = s.ResumeHeadlineBn,
        ResumeButtonLabel = s.ResumeButtonLabel,
        ResumeButtonLabelBn = s.ResumeButtonLabelBn,
        ShowResumeSection = s.ShowResumeSection
    };

    public void ApplyTo(SiteSettings s)
    {
        s.ResumeMediaId = ResumeMediaId;
        s.ResumeHeadline = string.IsNullOrWhiteSpace(ResumeHeadline) ? null : ResumeHeadline.Trim();
        s.ResumeHeadlineBn = string.IsNullOrWhiteSpace(ResumeHeadlineBn) ? null : ResumeHeadlineBn.Trim();
        s.ResumeButtonLabel = string.IsNullOrWhiteSpace(ResumeButtonLabel) ? "Download CV" : ResumeButtonLabel.Trim();
        s.ResumeButtonLabelBn = string.IsNullOrWhiteSpace(ResumeButtonLabelBn) ? null : ResumeButtonLabelBn.Trim();
        s.ShowResumeSection = ShowResumeSection;
    }
}

public abstract class PublishableFormModel
{
    public int Id { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; }
}

public class AboutFormModel : PublishableFormModel
{
    [Required]
    [StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [StringLength(200)] public string? TitleBn { get; set; }
    public string? Body { get; set; }
    public string? BodyBn { get; set; }
    public int? ImageMediaId { get; set; }
    public IFormFile? ImageFile { get; set; }
    public bool IsVisible { get; set; } = true;

    public static AboutFormModel From(AboutSection a) => new()
    {
        Id = a.Id,
        Title = a.Title,
        TitleBn = a.TitleBn,
        Body = a.Body,
        BodyBn = a.BodyBn,
        ImageMediaId = a.ImageMediaId,
        IsVisible = a.IsVisible,
        Status = a.Status,
        DisplayOrder = a.DisplayOrder
    };

    public void ApplyTo(AboutSection a)
    {
        a.Title = Title.Trim();
        a.TitleBn = Clean(TitleBn);
        a.Body = Clean(Body);
        a.BodyBn = Clean(BodyBn);
        a.ImageMediaId = ImageMediaId;
        a.IsVisible = IsVisible;
        a.Status = Status;
        a.DisplayOrder = DisplayOrder;
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public class SkillCategoryFormModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(120)] public string? NameBn { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    public static SkillCategoryFormModel From(SkillCategory c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        NameBn = c.NameBn,
        DisplayOrder = c.DisplayOrder,
        IsVisible = c.IsVisible
    };
}

public class SkillFormModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(120)] public string? NameBn { get; set; }
    [StringLength(500)] public string? Description { get; set; }
    [StringLength(600)] public string? DescriptionBn { get; set; }
    public int? CategoryId { get; set; }
    [StringLength(60)] public string? IconName { get; set; }
    [Range(0, 100)] public int? Proficiency { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    public static SkillFormModel From(Skill s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        NameBn = s.NameBn,
        Description = s.Description,
        DescriptionBn = s.DescriptionBn,
        CategoryId = s.CategoryId,
        IconName = s.IconName,
        Proficiency = s.Proficiency,
        DisplayOrder = s.DisplayOrder,
        IsVisible = s.IsVisible
    };

    public void ApplyTo(Skill s)
    {
        s.Name = Name.Trim();
        s.NameBn = Clean(NameBn);
        s.Description = Clean(Description);
        s.DescriptionBn = Clean(DescriptionBn);
        s.CategoryId = CategoryId;
        s.IconName = Clean(IconName);
        s.Proficiency = Proficiency;
        s.DisplayOrder = DisplayOrder;
        s.IsVisible = IsVisible;
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public class ProjectFormModel : PublishableFormModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(240)] public string? TitleBn { get; set; }

    [RegularExpression("^$|^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Use lowercase letters, numbers and single hyphens only.")]
    [StringLength(160)]
    public string? Slug { get; set; }

    [StringLength(500)] public string? Summary { get; set; }
    [StringLength(600)] public string? SummaryBn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionBn { get; set; }
    public string? ProblemStatement { get; set; }
    public string? Solution { get; set; }
    public string? Features { get; set; }
    [StringLength(80)] public string? Category { get; set; }
    public int? CoverImageMediaId { get; set; }
    public IFormFile? CoverImageFile { get; set; }

    [UrlOrPath]
    [StringLength(500)]
    public string? RepositoryUrl { get; set; }

    [UrlOrPath]
    [StringLength(500)]
    public string? LiveUrl { get; set; }

    [DataType(DataType.Date)] public DateTime? StartDate { get; set; }
    [DataType(DataType.Date)] public DateTime? EndDate { get; set; }
    public ProjectStage Stage { get; set; } = ProjectStage.Completed;

    [StringLength(200)] public string? SeoTitle { get; set; }
    [StringLength(400)] public string? SeoDescription { get; set; }

    /// <summary>Comma-separated technology names, resolved to the shared technology catalogue.</summary>
    [StringLength(400)]
    public string? TechnologiesCsv { get; set; }

    public List<IFormFile> GalleryUploads { get; set; } = [];

    public static ProjectFormModel From(Project p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        TitleBn = p.TitleBn,
        Slug = p.Slug,
        Summary = p.Summary,
        SummaryBn = p.SummaryBn,
        Description = p.Description,
        DescriptionBn = p.DescriptionBn,
        ProblemStatement = p.ProblemStatement,
        Solution = p.Solution,
        Features = p.Features,
        Category = p.Category,
        CoverImageMediaId = p.CoverImageMediaId,
        RepositoryUrl = p.RepositoryUrl,
        LiveUrl = p.LiveUrl,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        Stage = p.Stage,
        SeoTitle = p.SeoTitle,
        SeoDescription = p.SeoDescription,
        TechnologiesCsv = string.Join(", ", p.Technologies.Select(t => t.Technology?.Name).Where(n => n is not null)),
        Status = p.Status,
        DisplayOrder = p.DisplayOrder,
        IsFeatured = p.IsFeatured
    };

    public void ApplyTo(Project p)
    {
        p.Title = Title.Trim();
        p.TitleBn = Clean(TitleBn);
        p.Summary = Clean(Summary);
        p.SummaryBn = Clean(SummaryBn);
        p.Description = Clean(Description);
        p.DescriptionBn = Clean(DescriptionBn);
        p.ProblemStatement = Clean(ProblemStatement);
        p.Solution = Clean(Solution);
        p.Features = Clean(Features);
        p.Category = Clean(Category);
        p.CoverImageMediaId = CoverImageMediaId;
        p.RepositoryUrl = Clean(RepositoryUrl);
        p.LiveUrl = Clean(LiveUrl);
        p.StartDate = StartDate;
        p.EndDate = EndDate;
        p.Stage = Stage;
        p.SeoTitle = Clean(SeoTitle);
        p.SeoDescription = Clean(SeoDescription);
        p.DisplayOrder = DisplayOrder;
        p.IsFeatured = IsFeatured;
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public class ExperienceFormModel : PublishableFormModel
{
    [Required]
    [StringLength(160)]
    public string JobTitle { get; set; } = string.Empty;

    [StringLength(200)] public string? JobTitleBn { get; set; }

    [Required]
    [StringLength(160)]
    public string Company { get; set; } = string.Empty;

    [StringLength(200)] public string? CompanyBn { get; set; }

    [UrlOrPath]
    [StringLength(500)]
    public string? CompanyUrl { get; set; }

    [StringLength(60)] public string? EmploymentType { get; set; }
    [StringLength(160)] public string? Location { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)] public DateTime? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Responsibilities { get; set; }
    public string? ResponsibilitiesBn { get; set; }
    public string? Achievements { get; set; }
    public string? AchievementsBn { get; set; }
    [StringLength(400)] public string? Technologies { get; set; }

    public static ExperienceFormModel From(Experience e) => new()
    {
        Id = e.Id,
        JobTitle = e.JobTitle,
        JobTitleBn = e.JobTitleBn,
        Company = e.Company,
        CompanyBn = e.CompanyBn,
        CompanyUrl = e.CompanyUrl,
        EmploymentType = e.EmploymentType,
        Location = e.Location,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        IsCurrent = e.IsCurrent,
        Responsibilities = e.Responsibilities,
        ResponsibilitiesBn = e.ResponsibilitiesBn,
        Achievements = e.Achievements,
        AchievementsBn = e.AchievementsBn,
        Technologies = e.Technologies,
        Status = e.Status,
        DisplayOrder = e.DisplayOrder
    };

    public void ApplyTo(Experience e)
    {
        e.JobTitle = JobTitle.Trim();
        e.JobTitleBn = Clean(JobTitleBn);
        e.Company = Company.Trim();
        e.CompanyBn = Clean(CompanyBn);
        e.CompanyUrl = Clean(CompanyUrl);
        e.EmploymentType = Clean(EmploymentType);
        e.Location = Clean(Location);
        e.StartDate = StartDate;
        e.EndDate = IsCurrent ? null : EndDate;
        e.IsCurrent = IsCurrent;
        e.Responsibilities = Clean(Responsibilities);
        e.ResponsibilitiesBn = Clean(ResponsibilitiesBn);
        e.Achievements = Clean(Achievements);
        e.AchievementsBn = Clean(AchievementsBn);
        e.Technologies = Clean(Technologies);
        e.DisplayOrder = DisplayOrder;
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public class EducationFormModel : PublishableFormModel
{
    [Required]
    [StringLength(200)]
    public string Institution { get; set; } = string.Empty;

    [StringLength(240)] public string? InstitutionBn { get; set; }
    [StringLength(160)] public string? Qualification { get; set; }
    [StringLength(200)] public string? QualificationBn { get; set; }
    [StringLength(160)] public string? Subject { get; set; }
    [DataType(DataType.Date)] public DateTime? StartDate { get; set; }
    [DataType(DataType.Date)] public DateTime? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    [StringLength(80)] public string? Result { get; set; }
    public string? Description { get; set; }
    public string? DescriptionBn { get; set; }
    public string? Achievements { get; set; }

    public static EducationFormModel From(Education e) => new()
    {
        Id = e.Id,
        Institution = e.Institution,
        InstitutionBn = e.InstitutionBn,
        Qualification = e.Qualification,
        QualificationBn = e.QualificationBn,
        Subject = e.Subject,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        IsCurrent = e.IsCurrent,
        Result = e.Result,
        Description = e.Description,
        DescriptionBn = e.DescriptionBn,
        Achievements = e.Achievements,
        Status = e.Status,
        DisplayOrder = e.DisplayOrder
    };

    public void ApplyTo(Education e)
    {
        e.Institution = Institution.Trim();
        e.InstitutionBn = Clean(InstitutionBn);
        e.Qualification = Clean(Qualification);
        e.QualificationBn = Clean(QualificationBn);
        e.Subject = Clean(Subject);
        e.StartDate = StartDate;
        e.EndDate = IsCurrent ? null : EndDate;
        e.IsCurrent = IsCurrent;
        e.Result = Clean(Result);
        e.Description = Clean(Description);
        e.DescriptionBn = Clean(DescriptionBn);
        e.Achievements = Clean(Achievements);
        e.DisplayOrder = DisplayOrder;
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public class ResearchFormModel : PublishableFormModel
{
    [Required]
    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [StringLength(300)] public string? TitleBn { get; set; }

    [RegularExpression("^$|^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Use lowercase letters, numbers and single hyphens only.")]
    [StringLength(180)]
    public string? Slug { get; set; }

    public string? Abstract { get; set; }
    public string? AbstractBn { get; set; }
    public string? Introduction { get; set; }
    public string? Methodology { get; set; }
    [StringLength(600)] public string? Tools { get; set; }
    [StringLength(600)] public string? Datasets { get; set; }
    public string? Experiments { get; set; }
    public string? Findings { get; set; }
    public string? Discussion { get; set; }
    public string? Limitations { get; set; }
    public string? Conclusion { get; set; }
    public string? FutureWork { get; set; }
    public string? References { get; set; }
    [StringLength(500)] public string? Keywords { get; set; }

    public int? CategoryId { get; set; }

    [DataType(DataType.Date)] public DateTime? StartDate { get; set; }
    [DataType(DataType.Date)] public DateTime? PublicationDate { get; set; }

    [StringLength(250)] public string? PublicationVenue { get; set; }
    [StringLength(250)] public string? Journal { get; set; }
    [StringLength(200)] public string? Doi { get; set; }

    [UrlOrPath]
    [StringLength(600)]
    public string? ExternalUrl { get; set; }

    [StringLength(200)] public string? SeoTitle { get; set; }
    [StringLength(400)] public string? SeoDescription { get; set; }

    public int? CoverImageMediaId { get; set; }
    public IFormFile? CoverImageFile { get; set; }
    public int? PdfMediaId { get; set; }
    public IFormFile? PdfFile { get; set; }

    /// <summary>One author per line: "Name | Affiliation | Role".</summary>
    public string? AuthorsText { get; set; }

    /// <summary>Comma-separated tag names.</summary>
    [StringLength(400)]
    public string? TagsCsv { get; set; }

    public List<int> RelatedIds { get; set; } = [];

    public static ResearchFormModel From(Research r) => new()
    {
        Id = r.Id,
        Title = r.Title,
        TitleBn = r.TitleBn,
        Slug = r.Slug,
        Abstract = r.Abstract,
        AbstractBn = r.AbstractBn,
        Introduction = r.Introduction,
        Methodology = r.Methodology,
        Tools = r.Tools,
        Datasets = r.Datasets,
        Experiments = r.Experiments,
        Findings = r.Findings,
        Discussion = r.Discussion,
        Limitations = r.Limitations,
        Conclusion = r.Conclusion,
        FutureWork = r.FutureWork,
        References = r.References,
        Keywords = r.Keywords,
        CategoryId = r.CategoryId,
        StartDate = r.StartDate,
        PublicationDate = r.PublicationDate,
        PublicationVenue = r.PublicationVenue,
        Journal = r.Journal,
        Doi = r.Doi,
        ExternalUrl = r.ExternalUrl,
        SeoTitle = r.SeoTitle,
        SeoDescription = r.SeoDescription,
        CoverImageMediaId = r.CoverImageMediaId,
        PdfMediaId = r.PdfMediaId,
        AuthorsText = string.Join(Environment.NewLine, r.Authors
            .OrderBy(a => a.DisplayOrder)
            .Select(a => string.Join(" | ", new[] { a.Name, a.Affiliation, a.Role }.Where(x => !string.IsNullOrWhiteSpace(x))))),
        TagsCsv = string.Join(", ", r.Tags.Select(t => t.Tag?.Name).Where(n => n is not null)),
        RelatedIds = r.Related.Select(x => x.RelatedResearchId).ToList(),
        Status = r.Status,
        DisplayOrder = r.DisplayOrder,
        IsFeatured = r.IsFeatured
    };

    public void ApplyTo(Research r)
    {
        r.Title = Title.Trim();
        r.TitleBn = Clean(TitleBn);
        r.Abstract = Clean(Abstract);
        r.AbstractBn = Clean(AbstractBn);
        r.Introduction = Clean(Introduction);
        r.Methodology = Clean(Methodology);
        r.Tools = Clean(Tools);
        r.Datasets = Clean(Datasets);
        r.Experiments = Clean(Experiments);
        r.Findings = Clean(Findings);
        r.Discussion = Clean(Discussion);
        r.Limitations = Clean(Limitations);
        r.Conclusion = Clean(Conclusion);
        r.FutureWork = Clean(FutureWork);
        r.References = Clean(References);
        r.Keywords = Clean(Keywords);
        r.CategoryId = CategoryId;
        r.StartDate = StartDate;
        r.PublicationDate = PublicationDate;
        r.PublicationVenue = Clean(PublicationVenue);
        r.Journal = Clean(Journal);
        r.Doi = Clean(Doi);
        r.ExternalUrl = Clean(ExternalUrl);
        r.SeoTitle = Clean(SeoTitle);
        r.SeoDescription = Clean(SeoDescription);
        r.CoverImageMediaId = CoverImageMediaId;
        r.PdfMediaId = PdfMediaId;
        r.DisplayOrder = DisplayOrder;
        r.IsFeatured = IsFeatured;
    }

    public IReadOnlyList<ParsedAuthor> ParseAuthors()
    {
        if (string.IsNullOrWhiteSpace(AuthorsText)) return [];
        var list = new List<ParsedAuthor>();
        foreach (var line in AuthorsText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(25))
        {
            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0])) continue;
            list.Add(new ParsedAuthor(parts[0], parts.ElementAtOrDefault(1), parts.ElementAtOrDefault(2)));
        }
        return list;
    }

    public IReadOnlyList<string> ParseTags() =>
        string.IsNullOrWhiteSpace(TagsCsv)
            ? []
            : TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList();

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public record ParsedAuthor(string Name, string? Affiliation, string? Role);

public class CertificationFormModel : PublishableFormModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(240)] public string? TitleBn { get; set; }

    [Required]
    [StringLength(200)]
    public string Issuer { get; set; } = string.Empty;

    [UrlOrPath]
    [StringLength(600)]
    public string? IssuerUrl { get; set; }

    [DataType(DataType.Date)] public DateTime? IssueDate { get; set; }
    [DataType(DataType.Date)] public DateTime? ExpiryDate { get; set; }
    [StringLength(160)] public string? CredentialId { get; set; }

    [UrlOrPath]
    [StringLength(600)]
    public string? VerificationUrl { get; set; }

    public int? ImageMediaId { get; set; }
    public IFormFile? ImageFile { get; set; }
    public string? Description { get; set; }
    public string? DescriptionBn { get; set; }

    public static CertificationFormModel From(Certification c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        TitleBn = c.TitleBn,
        Issuer = c.Issuer,
        IssuerUrl = c.IssuerUrl,
        IssueDate = c.IssueDate,
        ExpiryDate = c.ExpiryDate,
        CredentialId = c.CredentialId,
        VerificationUrl = c.VerificationUrl,
        ImageMediaId = c.ImageMediaId,
        Description = c.Description,
        DescriptionBn = c.DescriptionBn,
        Status = c.Status,
        DisplayOrder = c.DisplayOrder
    };

    public void ApplyTo(Certification c)
    {
        c.Title = Title.Trim();
        c.TitleBn = Clean(TitleBn);
        c.Issuer = Issuer.Trim();
        c.IssuerUrl = Clean(IssuerUrl);
        c.IssueDate = IssueDate;
        c.ExpiryDate = ExpiryDate;
        c.CredentialId = Clean(CredentialId);
        c.VerificationUrl = Clean(VerificationUrl);
        c.ImageMediaId = ImageMediaId;
        c.Description = Clean(Description);
        c.DescriptionBn = Clean(DescriptionBn);
        c.DisplayOrder = DisplayOrder;
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public class AchievementFormModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(240)] public string? TitleBn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionBn { get; set; }
    [DataType(DataType.Date)] public DateTime? AchievementDate { get; set; }
    [StringLength(200)] public string? IssuingOrganization { get; set; }
    public int? ImageMediaId { get; set; }
    public IFormFile? ImageFile { get; set; }

    [UrlOrPath]
    [StringLength(600)]
    public string? ExternalUrl { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    public static AchievementFormModel From(Achievement a) => new()
    {
        Id = a.Id,
        Title = a.Title,
        TitleBn = a.TitleBn,
        Description = a.Description,
        DescriptionBn = a.DescriptionBn,
        AchievementDate = a.AchievementDate,
        IssuingOrganization = a.IssuingOrganization,
        ImageMediaId = a.ImageMediaId,
        ExternalUrl = a.ExternalUrl,
        DisplayOrder = a.DisplayOrder,
        IsVisible = a.IsVisible
    };

    public void ApplyTo(Achievement a)
    {
        a.Title = Title.Trim();
        a.TitleBn = Clean(TitleBn);
        a.Description = Clean(Description);
        a.DescriptionBn = Clean(DescriptionBn);
        a.AchievementDate = AchievementDate;
        a.IssuingOrganization = Clean(IssuingOrganization);
        a.ImageMediaId = ImageMediaId;
        a.ExternalUrl = Clean(ExternalUrl);
        a.DisplayOrder = DisplayOrder;
        a.IsVisible = IsVisible;
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public class ServiceFormModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [StringLength(200)] public string? TitleBn { get; set; }
    [StringLength(1000)] public string? Description { get; set; }
    [StringLength(1200)] public string? DescriptionBn { get; set; }
    [StringLength(60)] public string? IconName { get; set; }

    [Range(0, 100000000)]
    public decimal? StartingPrice { get; set; }

    [StringLength(10)] public string? Currency { get; set; }

    [StringLength(60)] public string? CtaLabel { get; set; }

    [UrlOrPath]
    [StringLength(300)]
    public string? CtaUrl { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    public static ServiceFormModel From(Service s) => new()
    {
        Id = s.Id,
        Title = s.Title,
        TitleBn = s.TitleBn,
        Description = s.Description,
        DescriptionBn = s.DescriptionBn,
        IconName = s.IconName,
        StartingPrice = s.StartingPrice,
        Currency = s.Currency,
        CtaLabel = s.CtaLabel,
        CtaUrl = s.CtaUrl,
        DisplayOrder = s.DisplayOrder,
        IsVisible = s.IsVisible
    };

    public void ApplyTo(Service s)
    {
        s.Title = Title.Trim();
        s.TitleBn = Clean(TitleBn);
        s.Description = Clean(Description);
        s.DescriptionBn = Clean(DescriptionBn);
        s.IconName = Clean(IconName);
        s.StartingPrice = StartingPrice;
        s.Currency = Clean(Currency);
        s.CtaLabel = Clean(CtaLabel);
        s.CtaUrl = Clean(CtaUrl);
        s.DisplayOrder = DisplayOrder;
        s.IsVisible = IsVisible;
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public class TestimonialFormModel : PublishableFormModel
{
    [Required]
    [StringLength(160)]
    public string ClientName { get; set; } = string.Empty;

    [StringLength(160)] public string? ClientRole { get; set; }
    [StringLength(200)] public string? Organization { get; set; }

    [Required]
    [StringLength(2000)]
    public string Quote { get; set; } = string.Empty;

    [StringLength(2400)] public string? QuoteBn { get; set; }
    public int? AvatarMediaId { get; set; }
    public IFormFile? AvatarFile { get; set; }
    [DataType(DataType.Date)] public DateTime? TestimonialDate { get; set; }

    [UrlOrPath]
    [StringLength(600)]
    public string? SourceUrl { get; set; }

    public static TestimonialFormModel From(Testimonial t) => new()
    {
        Id = t.Id,
        ClientName = t.ClientName,
        ClientRole = t.ClientRole,
        Organization = t.Organization,
        Quote = t.Quote,
        QuoteBn = t.QuoteBn,
        AvatarMediaId = t.AvatarMediaId,
        TestimonialDate = t.TestimonialDate,
        SourceUrl = t.SourceUrl,
        Status = t.Status,
        DisplayOrder = t.DisplayOrder
    };

    public void ApplyTo(Testimonial t)
    {
        t.ClientName = ClientName.Trim();
        t.ClientRole = Clean(ClientRole);
        t.Organization = Clean(Organization);
        t.Quote = Quote.Trim();
        t.QuoteBn = Clean(QuoteBn);
        t.AvatarMediaId = AvatarMediaId;
        t.TestimonialDate = TestimonialDate;
        t.SourceUrl = Clean(SourceUrl);
        t.DisplayOrder = DisplayOrder;
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

public class SocialLinkFormModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    public string Platform { get; set; } = string.Empty;

    [Required]
    [Url]
    [StringLength(600)]
    public string Url { get; set; } = string.Empty;

    [StringLength(60)] public string? IconName { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool ShowInHero { get; set; } = true;
    public bool ShowInFooter { get; set; } = true;

    public static SocialLinkFormModel From(SocialLink s) => new()
    {
        Id = s.Id,
        Platform = s.Platform,
        Url = s.Url,
        IconName = s.IconName,
        DisplayOrder = s.DisplayOrder,
        IsVisible = s.IsVisible,
        ShowInHero = s.ShowInHero,
        ShowInFooter = s.ShowInFooter
    };
}

public class NavigationFormModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    public string Label { get; set; } = string.Empty;

    [StringLength(100)] public string? LabelBn { get; set; }

    [Required]
    [UrlOrPath]
    [StringLength(300)]
    public string Url { get; set; } = "/";

    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool OpenInNewTab { get; set; }
    public bool ShowInFooter { get; set; }

    [StringLength(40)]
    public string? SectionKey { get; set; }

    public static NavigationFormModel From(NavigationItem n) => new()
    {
        Id = n.Id,
        Label = n.Label,
        LabelBn = n.LabelBn,
        Url = n.Url,
        DisplayOrder = n.DisplayOrder,
        IsVisible = n.IsVisible,
        OpenInNewTab = n.OpenInNewTab,
        ShowInFooter = n.ShowInFooter,
        SectionKey = n.SectionKey
    };
}

public class MediaUploadViewModel
{
    [Required]
    public IFormFile? File { get; set; }

    public MediaKind Kind { get; set; } = MediaKind.Image;

    [StringLength(250)]
    public string? AltText { get; set; }
}

public class ResearchCategoryFormModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(120)] public string? NameBn { get; set; }

    [RegularExpression("^$|^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Use lowercase letters, numbers and single hyphens only.")]
    [StringLength(120)]
    public string? Slug { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
}

public class DashboardViewModel
{
    public SiteSettings Settings { get; init; } = new();
    public Profile Profile { get; init; } = new();
    public int TotalProjects { get; init; }
    public int PublishedProjects { get; init; }
    public int DraftProjects { get; init; }
    public int TotalResearch { get; init; }
    public int PublishedResearch { get; init; }
    public int DraftResearch { get; init; }
    public int TotalSkills { get; init; }
    public int TotalCertifications { get; init; }
    public int TotalExperience { get; init; }
    public int TotalEducation { get; init; }
    public int PendingTestimonials { get; init; }
    public int PublishedTestimonials { get; init; }
    public int TotalServices { get; init; }
    public int MediaFiles { get; init; }
    public int UnreadMessages { get; init; }
    public int TotalMessages { get; init; }
    public int PublishedAboutSections { get; init; }
    public IReadOnlyList<RecentActivity> Recent { get; init; } = [];
    public bool ProfileIncomplete => string.IsNullOrWhiteSpace(Profile.FullName);
}

public record RecentActivity(string Title, string Type, DateTime When, string? AdminPath);
