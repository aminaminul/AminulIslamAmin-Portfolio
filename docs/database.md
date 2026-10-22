# Database

Microsoft SQL Server is the only supported production provider
(`Microsoft.EntityFrameworkCore.SqlServer`). SQLite is used by the automated tests only.

## 1. Connection and schema management

- Connection string: `ConnectionStrings:DefaultConnection` (user secrets in development,
  environment variable `ConnectionStrings__DefaultConnection` on a host). Startup refuses to run
  without it and logs an actionable message instead of crashing on a random page.
- Migrations live in `src/Portfolio.Web/Data/Migrations` (`InitialCreate` is the baseline).
- Development applies them automatically (`Database:MigrateOnStartup=true` in
  `appsettings.Development.json`). Production defaults to `false`, so schema changes are applied
  deliberately:

```bash
dotnet ef migrations add <Name> --project src/Portfolio.Web
dotnet ef database update --project src/Portfolio.Web
dotnet ef migrations script --idempotent --project src/Portfolio.Web --output migrate.sql
```

`DesignTimeDbContextFactory` reads the same configuration so `dotnet ef` works without running the
web host. The idempotent script is the safe way to update a hosted database: review it, then run it
from a maintenance window.

## 2. Tables

Identity tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`,
`AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`) come from ASP.NET Core Identity.
`ApplicationUser` adds `DisplayName` and `CreatedAt`.

| Table | Kind | Notable columns |
| --- | --- | --- |
| `SiteSettings` | single row | `SiteName`, `Tagline`, `LogoMediaId`, `ContactEmail`, `ContactPhone`, `ContactLocation`, `ShowContactForm`, `ContactFormRecipient`, `ResumeMediaId`, `ResumeButtonLabel`, `ShowResumeSection`, `SeoTitle`, `SeoDescription`, `SeoKeywords`, `OgImageMediaId`, `DefaultTheme`, `AccentColor`, `RobotsTxtExtra`, `IsAvailable`, `AvailabilityText` |
| `Profiles` | single row | `FullName` (required, 160), `DisplayName`, `ProfessionalTitle(Bn)`, `ShortIntro(Bn)`, `Location`, `PhotoMediaId`, `PhotoAlt` |
| `HeroSettings` | single row | `BadgeText(Bn)`, `Headline(Bn)`, `Subheadline(Bn)`, `ImageMediaId`, `PrimaryCtaLabel/Url`, `SecondaryCtaLabel/Url`, `ShowPrimaryCta`, `ShowSecondaryCta`, `ShowResumeButton`, `ShowSocialLinks`, `ShowPhoto`, `ImagePosition` |
| `AboutSections` | ordered, publishable | `Title(Bn)`, `Body(Bn)` (Markdown), `ImageMediaId`, `IsVisible`, `Status`, `DisplayOrder` |
| `SkillCategories` | ordered | `Name(Bn)`, `IsVisible`, `DisplayOrder` |
| `Skills` | ordered | `Name(Bn)`, `Description(Bn)`, `CategoryId`, `IconName`, `Proficiency` (0-100, nullable — hides the indicator), `IsVisible`, `DisplayOrder` |
| `Projects` | ordered, publishable | `Title(Bn)`, `Slug` (unique), `Summary(Bn)`, `Description(Bn)`, `ProblemStatement`, `Solution`, `Features`, `Category`, `CoverImageMediaId`, `RepositoryUrl`, `LiveUrl`, `StartDate`, `EndDate`, `Stage`, `IsFeatured`, `Status`, `SeoTitle`, `SeoDescription` |
| `ProjectImages` | gallery | `ProjectId`, `MediaFileId`, `Caption`, `DisplayOrder` |
| `Technologies` / `ProjectTechnology` | catalogue + join | `Name`, `Slug` (unique) |
| `Experiences` | ordered, publishable | `JobTitle(Bn)`, `Company(Bn)`, `CompanyUrl`, `EmploymentType`, `Location`, `StartDate`, `EndDate`, `IsCurrent`, `Responsibilities(Bn)`, `Achievements(Bn)`, `Technologies` |
| `Educations` | ordered, publishable | `Institution(Bn)`, `Qualification(Bn)`, `Subject`, `StartDate`, `EndDate`, `IsCurrent`, `Result`, `Description(Bn)`, `Achievements` |
| `Research` | ordered, publishable | `Title(Bn)`, `Slug` (unique), `Abstract(Bn)`, `Introduction`, `Methodology`, `Tools`, `Datasets`, `Experiments`, `Findings`, `Discussion`, `Limitations`, `Conclusion`, `FutureWork`, `References`, `Keywords`, `CategoryId`, `StartDate`, `PublicationDate`, `PublicationVenue`, `Journal`, `Doi`, `ExternalUrl`, `CoverImageMediaId`, `PdfMediaId`, SEO fields |
| `ResearchCategories`, `ResearchAuthors`, `Tags`, `ResearchTag`, `ResearchRelated` | taxonomy | ordered lists and joins |
| `Certifications` | ordered, publishable | `Title(Bn)`, `Issuer`, `IssuerUrl`, `IssueDate`, `ExpiryDate`, `CredentialId`, `VerificationUrl`, `ImageMediaId`, `Description(Bn)` |
| `Achievements` | ordered | `Title(Bn)`, `Description(Bn)`, `AchievementDate`, `IssuingOrganization`, `ImageMediaId`, `ExternalUrl`, `IsVisible` |
| `Services` | ordered | `Title(Bn)`, `Description(Bn)`, `IconName`, `StartingPrice`, `Currency`, `CtaLabel`, `CtaUrl`, `IsVisible` |
| `Testimonials` | ordered, publishable | `ClientName`, `ClientRole`, `Organization`, `Quote(Bn)`, `AvatarMediaId`, `TestimonialDate`, `SourceUrl`; published = approved |
| `SocialLinks` | ordered | `Platform`, `Url`, `IconName`, `ShowInHero`, `ShowInFooter`, `IsVisible` |
| `NavigationItems` | ordered | `Label(Bn)`, `Url`, `SectionKey` (feature gate), `ShowInFooter`, `OpenInNewTab`, `IsVisible` |
| `MediaFiles` | metadata | `OriginalName`, `StorageName` (unique), `ContentType`, `SizeBytes`, `Extension`, `Kind`, `AltText`, `UploadedBy` |
| `ContactMessages` | append-only log | `Name`, `Email`, `Subject`, `Message`, `IpHash`, `IsRead`, `IsArchived`, `EmailState`, `EmailError`, `CreatedAt` |

`BaseEntity` supplies `Id`, `CreatedAt`, `UpdatedAt` (UTC, maintained in `SaveChanges`).
`PublishableEntity` adds `Status`, `PublishedAt`, `IsFeatured`, `DisplayOrder`.

## 3. Integrity rules

- **Unique:** `Projects.Slug`, `Research.Slug`, `Technology.Slug`, `ResearchCategory.Slug`, `Tag.Slug`,
  `MediaFiles.StorageName`. Slugs are generated by `SlugService` (diacritics stripped, lowercase,
  non-alphanumerics collapsed) and de-duplicated with `-2`, `-3`, … before save, so the constraint is
  a safety net rather than a user-facing error.
- **Length limits** on every string column (for example `FullName` 160, `ShortIntro` 500,
  `SeoDescription` 400) matching the form validation attributes — no silent truncation.
- **Delete behaviour:** media relationships use `DeleteBehavior.NoAction` so deleting a media record
  cannot cascade into content rows; project images, research tags/related links and author rows use
  cascade within their own aggregate. Deleting a project never touches unrelated content.
  Deleting a `MediaFile` that is still referenced from a content row is prevented in the service
  layer, which reports the conflict instead of leaving a dangling id.
- **Contact messages** are never modified except for `IsRead` / `IsArchived`; there is no public read
  path.

## 4. Indexes

27 indexes support the actual query patterns:

| Purpose | Indexes |
| --- | --- |
| Public listings (status + order) | `Projects(Status, DisplayOrder)`, `Research(Status, DisplayOrder)`, `AboutSections`, `Experiences`, `Educations`, `Certifications`, `Testimonials` |
| Visibility flags | `SkillCategories(IsVisible, DisplayOrder)`, `Skills`, `Achievements`, `Services`, `SocialLinks`, `NavigationItems` |
| Detail lookups | unique slug indexes on `Projects`, `Research`, `Technologies`, `ResearchCategories`, `Tags` |
| Sorting by time | `Projects(PublishedAt)`, `Research(PublishedAt)`, `Research(PublicationDate)`, `Experiences(StartDate)` |
| Gallery ordering | `ProjectImages(ProjectId, DisplayOrder)`, `ResearchAuthors(ResearchId, DisplayOrder)` |
| Media | unique `StorageName` |
| Inbox | `ContactMessages(CreatedAt)`, `ContactMessages(IsArchived, IsRead)` |

Queries are projected (`Select`) where only a few columns are needed and always paged through
`PagedList<T>` (public page size 9, admin 12) so growing collections do not load whole tables.
Records that load several collection navigations (project gallery + technologies, research authors
+ tags + related) use `AsSplitQuery()` so the row count does not multiply across joins.

## 5. Public query contract

| Page | Filter |
| --- | --- |
| `/` | published featured projects and research, visible skills/services/testimonials, published experience/education/certification summaries |
| `/projects`, `/projects/{slug}` | `Status == Published`; detail returns 404 when the slug is missing *or* unpublished |
| `/research`, `/research/{slug}` | `Status == Published`, optional category/keyword/year filters; detail 404 otherwise |
| `/about` | `IsVisible` and `Status == Published` about sections |
| `/services` | `IsVisible` services only |
| `/sitemap.xml` | only published projects, published research and enabled static pages |
| `/contact` | form only when `ShowContactForm` is true |

Admin queries are identical but unrestricted, and only reachable behind
`[Authorize(Roles = "Administrator")]`.

## 6. Backup and migration safety

- Take a backup (or a `bacpac`/export) before applying a migration to a live database.
- Prefer additive migrations (new nullable columns or columns with defaults). Renaming or dropping a
  column is a destructive change: migrate data first, deploy code that no longer reads the old
  column, and only then drop it.
- Never run `Database:MigrateOnStartup=true` against production data you have not backed up; the
  documented production flow is the reviewed idempotent script.
