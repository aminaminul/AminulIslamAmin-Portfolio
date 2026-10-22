using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Models.Entities;

namespace Portfolio.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<HeroSettings> HeroSettings => Set<HeroSettings>();
    public DbSet<AboutSection> AboutSections => Set<AboutSection>();
    public DbSet<SkillCategory> SkillCategories => Set<SkillCategory>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectImage> ProjectImages => Set<ProjectImage>();
    public DbSet<Technology> Technologies => Set<Technology>();
    public DbSet<ProjectTechnology> ProjectTechnologies => Set<ProjectTechnology>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<Research> Research => Set<Research>();
    public DbSet<ResearchCategory> ResearchCategories => Set<ResearchCategory>();
    public DbSet<ResearchAuthor> ResearchAuthors => Set<ResearchAuthor>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ResearchTag> ResearchTags => Set<ResearchTag>();
    public DbSet<ResearchRelated> ResearchRelated => Set<ResearchRelated>();
    public DbSet<Certification> Certifications => Set<Certification>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<SocialLink> SocialLinks => Set<SocialLink>();
    public DbSet<NavigationItem> NavigationItems => Set<NavigationItem>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SiteSettings>(e =>
        {
            e.Property(x => x.SiteName).HasMaxLength(120).IsRequired();
            e.Property(x => x.SiteNameBn).HasMaxLength(120);
            e.Property(x => x.Tagline).HasMaxLength(200);
            e.Property(x => x.TaglineBn).HasMaxLength(200);
            e.Property(x => x.LogoText).HasMaxLength(60);
            e.Property(x => x.FooterDescription).HasMaxLength(600);
            e.Property(x => x.FooterDescriptionBn).HasMaxLength(600);
            e.Property(x => x.CopyrightText).HasMaxLength(200);
            e.Property(x => x.AvailabilityText).HasMaxLength(160);
            e.Property(x => x.AvailabilityTextBn).HasMaxLength(160);
            e.Property(x => x.ContactIntro).HasMaxLength(1000);
            e.Property(x => x.ContactIntroBn).HasMaxLength(1000);
            e.Property(x => x.ContactEmail).HasMaxLength(200);
            e.Property(x => x.ContactPhone).HasMaxLength(40);
            e.Property(x => x.ContactLocation).HasMaxLength(160);
            e.Property(x => x.ContactFormRecipient).HasMaxLength(200);
            e.Property(x => x.ResumeHeadline).HasMaxLength(400);
            e.Property(x => x.ResumeHeadlineBn).HasMaxLength(400);
            e.Property(x => x.ResumeButtonLabel).HasMaxLength(80);
            e.Property(x => x.ResumeButtonLabelBn).HasMaxLength(80);
            e.Property(x => x.SeoTitle).HasMaxLength(200);
            e.Property(x => x.SeoDescription).HasMaxLength(400);
            e.Property(x => x.SeoKeywords).HasMaxLength(400);
            e.Property(x => x.DefaultTheme).HasMaxLength(20);
            e.Property(x => x.AccentColor).HasMaxLength(20);
            e.Property(x => x.RobotsTxtExtra).HasMaxLength(1000);
        });

        builder.Entity<Profile>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(160).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(120);
            e.Property(x => x.ProfessionalTitle).HasMaxLength(160);
            e.Property(x => x.ProfessionalTitleBn).HasMaxLength(200);
            e.Property(x => x.ShortIntro).HasMaxLength(500);
            e.Property(x => x.ShortIntroBn).HasMaxLength(600);
            e.Property(x => x.Location).HasMaxLength(160);
            e.Property(x => x.PhotoAlt).HasMaxLength(250);
            e.HasOne(x => x.Photo).WithMany().HasForeignKey(x => x.PhotoMediaId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<HeroSettings>(e =>
        {
            e.Property(x => x.BadgeText).HasMaxLength(80);
            e.Property(x => x.BadgeTextBn).HasMaxLength(100);
            e.Property(x => x.Headline).HasMaxLength(200);
            e.Property(x => x.HeadlineBn).HasMaxLength(250);
            e.Property(x => x.Subheadline).HasMaxLength(600);
            e.Property(x => x.SubheadlineBn).HasMaxLength(700);
            e.Property(x => x.PrimaryCtaLabel).HasMaxLength(60);
            e.Property(x => x.PrimaryCtaLabelBn).HasMaxLength(80);
            e.Property(x => x.PrimaryCtaUrl).HasMaxLength(300);
            e.Property(x => x.SecondaryCtaLabel).HasMaxLength(60);
            e.Property(x => x.SecondaryCtaLabelBn).HasMaxLength(80);
            e.Property(x => x.SecondaryCtaUrl).HasMaxLength(300);
            e.Property(x => x.ImagePosition).HasMaxLength(20);
            e.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageMediaId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<AboutSection>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(160).IsRequired();
            e.Property(x => x.TitleBn).HasMaxLength(200);
            e.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageMediaId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.Status, x.DisplayOrder });
        });

        builder.Entity<SkillCategory>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.NameBn).HasMaxLength(120);
            e.HasIndex(x => new { x.IsVisible, x.DisplayOrder });
        });

        builder.Entity<Skill>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.NameBn).HasMaxLength(120);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.DescriptionBn).HasMaxLength(600);
            e.Property(x => x.IconName).HasMaxLength(60);
            e.HasOne(x => x.Category).WithMany(x => x.Skills).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.IsVisible, x.DisplayOrder });
        });

        builder.Entity<Project>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.TitleBn).HasMaxLength(240);
            e.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Summary).HasMaxLength(500);
            e.Property(x => x.SummaryBn).HasMaxLength(600);
            e.Property(x => x.Category).HasMaxLength(80);
            e.Property(x => x.RepositoryUrl).HasMaxLength(500);
            e.Property(x => x.LiveUrl).HasMaxLength(500);
            e.Property(x => x.SeoTitle).HasMaxLength(200);
            e.Property(x => x.SeoDescription).HasMaxLength(400);
            e.HasIndex(x => new { x.Status, x.DisplayOrder });
            e.HasIndex(x => x.PublishedAt);
            e.HasOne(x => x.CoverImage).WithMany().HasForeignKey(x => x.CoverImageMediaId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<ProjectImage>(e =>
        {
            e.Property(x => x.Caption).HasMaxLength(250);
            e.HasOne(x => x.Project).WithMany(x => x.Images).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.MediaFile).WithMany().HasForeignKey(x => x.MediaFileId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.ProjectId, x.DisplayOrder });
        });

        builder.Entity<Technology>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<ProjectTechnology>(e =>
        {
            e.HasKey(x => new { x.ProjectId, x.TechnologyId });
            e.HasOne(x => x.Project).WithMany(x => x.Technologies).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Technology).WithMany(x => x.Projects).HasForeignKey(x => x.TechnologyId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Experience>(e =>
        {
            e.Property(x => x.JobTitle).HasMaxLength(160).IsRequired();
            e.Property(x => x.JobTitleBn).HasMaxLength(200);
            e.Property(x => x.Company).HasMaxLength(160).IsRequired();
            e.Property(x => x.CompanyBn).HasMaxLength(200);
            e.Property(x => x.CompanyUrl).HasMaxLength(500);
            e.Property(x => x.EmploymentType).HasMaxLength(60);
            e.Property(x => x.Location).HasMaxLength(160);
            e.Property(x => x.Technologies).HasMaxLength(400);
            e.HasIndex(x => new { x.Status, x.DisplayOrder });
            e.HasIndex(x => x.StartDate);
        });

        builder.Entity<Education>(e =>
        {
            e.Property(x => x.Institution).HasMaxLength(200).IsRequired();
            e.Property(x => x.InstitutionBn).HasMaxLength(240);
            e.Property(x => x.Qualification).HasMaxLength(160);
            e.Property(x => x.QualificationBn).HasMaxLength(200);
            e.Property(x => x.Subject).HasMaxLength(160);
            e.Property(x => x.Result).HasMaxLength(80);
            e.HasIndex(x => new { x.Status, x.DisplayOrder });
        });

        builder.Entity<ResearchCategory>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.NameBn).HasMaxLength(120);
            e.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<Research>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(250).IsRequired();
            e.Property(x => x.TitleBn).HasMaxLength(300);
            e.Property(x => x.Slug).HasMaxLength(180).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Keywords).HasMaxLength(500);
            e.Property(x => x.PublicationVenue).HasMaxLength(250);
            e.Property(x => x.Journal).HasMaxLength(250);
            e.Property(x => x.Doi).HasMaxLength(200);
            e.Property(x => x.ExternalUrl).HasMaxLength(600);
            e.Property(x => x.SeoTitle).HasMaxLength(200);
            e.Property(x => x.SeoDescription).HasMaxLength(400);
            e.HasIndex(x => new { x.Status, x.DisplayOrder });
            e.HasIndex(x => x.PublishedAt);
            e.HasIndex(x => x.PublicationDate);
            e.HasOne(x => x.Category).WithMany(x => x.Records).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CoverImage).WithMany().HasForeignKey(x => x.CoverImageMediaId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Pdf).WithMany().HasForeignKey(x => x.PdfMediaId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<ResearchAuthor>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Affiliation).HasMaxLength(250);
            e.Property(x => x.Role).HasMaxLength(100);
            e.HasOne(x => x.Research).WithMany(x => x.Authors).HasForeignKey(x => x.ResearchId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ResearchId, x.DisplayOrder });
        });

        builder.Entity<Tag>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<ResearchTag>(e =>
        {
            e.HasKey(x => new { x.ResearchId, x.TagId });
            e.HasOne(x => x.Research).WithMany(x => x.Tags).HasForeignKey(x => x.ResearchId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag).WithMany(x => x.Research).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ResearchRelated>(e =>
        {
            e.HasKey(x => new { x.ResearchId, x.RelatedResearchId });
            e.HasOne(x => x.Research).WithMany(x => x.Related).HasForeignKey(x => x.ResearchId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RelatedResearch).WithMany().HasForeignKey(x => x.RelatedResearchId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Certification>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.TitleBn).HasMaxLength(240);
            e.Property(x => x.Issuer).HasMaxLength(200).IsRequired();
            e.Property(x => x.IssuerUrl).HasMaxLength(600);
            e.Property(x => x.CredentialId).HasMaxLength(160);
            e.Property(x => x.VerificationUrl).HasMaxLength(600);
            e.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageMediaId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.Status, x.DisplayOrder });
        });

        builder.Entity<Achievement>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.TitleBn).HasMaxLength(240);
            e.Property(x => x.IssuingOrganization).HasMaxLength(200);
            e.Property(x => x.ExternalUrl).HasMaxLength(600);
            e.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageMediaId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.IsVisible, x.DisplayOrder });
        });

        builder.Entity<Service>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(160).IsRequired();
            e.Property(x => x.TitleBn).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.DescriptionBn).HasMaxLength(1200);
            e.Property(x => x.IconName).HasMaxLength(60);
            e.Property(x => x.StartingPrice).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.CtaLabel).HasMaxLength(60);
            e.Property(x => x.CtaUrl).HasMaxLength(300);
            e.HasIndex(x => new { x.IsVisible, x.DisplayOrder });
        });

        builder.Entity<Testimonial>(e =>
        {
            e.Property(x => x.ClientName).HasMaxLength(160).IsRequired();
            e.Property(x => x.ClientRole).HasMaxLength(160);
            e.Property(x => x.Organization).HasMaxLength(200);
            e.Property(x => x.Quote).HasMaxLength(2000).IsRequired();
            e.Property(x => x.QuoteBn).HasMaxLength(2400);
            e.Property(x => x.SourceUrl).HasMaxLength(600);
            e.HasOne(x => x.Avatar).WithMany().HasForeignKey(x => x.AvatarMediaId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.Status, x.DisplayOrder });
        });

        builder.Entity<SocialLink>(e =>
        {
            e.Property(x => x.Platform).HasMaxLength(80).IsRequired();
            e.Property(x => x.Url).HasMaxLength(600).IsRequired();
            e.Property(x => x.IconName).HasMaxLength(60);
            e.HasIndex(x => new { x.IsVisible, x.DisplayOrder });
        });

        builder.Entity<NavigationItem>(e =>
        {
            e.Property(x => x.Label).HasMaxLength(80).IsRequired();
            e.Property(x => x.LabelBn).HasMaxLength(100);
            e.Property(x => x.Url).HasMaxLength(300).IsRequired();
            e.Property(x => x.SectionKey).HasMaxLength(40);
            e.HasIndex(x => new { x.IsVisible, x.DisplayOrder });
        });

        builder.Entity<MediaFile>(e =>
        {
            e.Property(x => x.StorageName).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.StorageName).IsUnique();
            e.Property(x => x.OriginalName).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            e.Property(x => x.Extension).HasMaxLength(12).IsRequired();
            e.Property(x => x.AltText).HasMaxLength(250);
            e.Property(x => x.UploadedBy).HasMaxLength(200);
        });

        builder.Entity<SiteSettings>(e =>
        {
            e.HasOne(x => x.Logo).WithMany().HasForeignKey(x => x.LogoMediaId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.OgImage).WithMany().HasForeignKey(x => x.OgImageMediaId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Resume).WithMany().HasForeignKey(x => x.ResumeMediaId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<ContactMessage>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(200);
            e.Property(x => x.Message).HasMaxLength(4000).IsRequired();
            e.Property(x => x.IpHash).HasMaxLength(64);
            e.Property(x => x.EmailError).HasMaxLength(400);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.IsArchived, x.IsRead });
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.DisplayName).HasMaxLength(120);
        });
    }

    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default) entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
