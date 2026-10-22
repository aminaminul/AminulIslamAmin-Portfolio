# Architecture

## 1. Shape of the application

A single ASP.NET Core MVC project (`src/Portfolio.Web`) contains both surfaces:

- **Public site** — conventional and attribute-routed controllers in `Controllers/`, rendering
  Razor views in `Views/`.
- **Admin panel** — an MVC *Area* (`Areas/Admin`) with its own controllers, view imports, layout,
  stylesheet and script. Every controller derives from `AdminControllerBase`, which carries
  `[Area("Admin")]` and `[Authorize(Roles = "Administrator")]`, so the protection cannot be forgotten
  on a new screen.

```
Browser
  │
  ├─ /                HomeController ─────────┐
  ├─ /projects        ProjectsController ─────┤
  ├─ /research        ResearchController ─────┤  IPortfolioQueryService
  ├─ /contact         ContactController ──────┤  IContactService
  ├─ /media/{id}      MediaController ────────┤  IMediaService
  ├─ /sitemap.xml     SitemapController ──────┤  ISitemapService
  │                                           ▼
  │                                       AppDbContext (EF Core) ── SQL Server
  └─ /Admin/**        Areas/Admin/*Controller ─┘
                          │
                          ├─ ISiteSettingsService (cached, invalidated on save)
                          ├─ ISiteChromeService   (navigation, footer, social links)
                          ├─ ISlugService         (unique, URL-safe slugs)
                          ├─ IMarkdownService     (Markdown → safe HTML)
                          └─ IMediaService        (validate, store, resolve, delete)
```

## 2. Responsibilities

| Layer | Location | Rules |
| --- | --- | --- |
| Controllers | `Controllers/`, `Areas/Admin/Controllers/` | HTTP only: bind, validate `ModelState`, call services/queries, choose a view or redirect. No business rules, no SQL beyond a simple load. |
| Services | `Services/` | Business logic and persistence rules: publishing, slug uniqueness, media validation, contact handling, sitemap generation, settings caching. |
| Entities | `Models/Entities/` | Persisted shape. `BaseEntity` (Id, CreatedAt, UpdatedAt), `PublishableEntity` (+ Status, PublishedAt, IsFeatured, DisplayOrder), `IVisibleContent`. |
| ViewModels | `Models/ViewModels/` | Public page models (`PublicViewModels.cs`), shared partial models (`SharedModels.cs`), admin forms with their validation attributes (`AdminForms.cs`). Entities are never posted back directly. |
| Views | `Views/`, `Areas/Admin/Views/` | Presentation only. Every value comes from a ViewModel; no personal data is hardcoded. |
| Configuration | `Models/Options/` | `MediaOptions`, `EmailOptions`, `AdminSeedOptions`, `DatabaseOptions`, `SiteOptions`, bound from configuration. |

Dependency injection is used throughout; every service is registered in `Program.cs`
(`AddScoped` for per-request work, `AddSingleton` for stateless helpers such as the slug and
Markdown services). All database access is asynchronous and cancellation-aware.

## 3. Publishing model

`PublishableEntity.Status` is an enum: `Draft`, `Published`, `Archived`. `PublishedAt` is stamped
when a record is published and cleared when it is unpublished.

The public queries live in `PortfolioQueryService` and every one of them starts from the same
filter:

```csharp
db.Projects.Where(p => p.Status == ContentStatus.Published)
```

Consequences, all covered by tests:

- Drafts and archived records are absent from listings, home sections, related-content blocks,
  sitemap entries, `robots`/`sitemap` output and detail URLs (detail returns **404**, not 403, so
  their existence is not disclosed).
- Visibility is decided on the server. No JavaScript hiding is involved.
- Unpublishing takes effect on the next request because no page-level output caching is used for
  content pages; the only cache is the single site-settings row, whose entry is invalidated on save.

## 4. Content model

```
Profile (1 row)          HeroSettings (1 row)      SiteSettings (1 row)
SiteSettings.Resume ─┐   Profile.Photo ─┐          SiteSettings.Logo, OgImage
                     ▼                  ▼
                  MediaFile ◄── Project.CoverImage, ProjectImage, AboutSection.Image,
                                Research.CoverImage, Certification.Image, Achievement.Image,
                                Testimonial.Avatar, Skill.Icon, HeroSettings.Image

SkillCategory 1─* Skill
Project 1─* ProjectImage
Project *─* Technology          (ProjectTechnology join)
Research *─* Tag                (ResearchTag join)
Research 1─* ResearchAuthor
Research *─* Project            (ResearchRelated join)
ResearchCategory 1─* Research
NavigationItem, SocialLink, Service, Testimonial, Certification, Experience, Education,
Achievement: ordered, visibility-controlled lists
ContactMessage: submissions with read/archive state and email delivery state
```

Ordering is explicit (`DisplayOrder`) and editable from the admin lists; uniqueness is enforced for
slugs. Full details in [database.md](database.md).

## 5. Admin panel conventions

- `AdminControllerBase` provides: current user id/name, `NotifySuccess` / `NotifyError` (TempData
  flash messages rendered by `_AdminToasts.cshtml`), `KeepOrReplaceAsync` (keeps the existing media
  id unless a new file is uploaded), media choice loading, `MoveAsync` (drag-free ordering with
  up/down buttons) and `SetPublishedAt`.
- List screens share `AdminListViewModel<T>`: paging, search, status filter, ordering controls and
  real POST forms for publish, unpublish, archive, feature, move and delete.
- Forms are separate ViewModels with DataAnnotations; the same model drives the create and edit
  views (`Form.cshtml`), so validation messages stay identical.
- Destructive actions are POST + antiforgery + a confirmation dialog in `admin.js`.
- Validation failures return the view with `ModelState` intact; entered values are preserved. No
  success message is shown unless the database write succeeded.

## 6. Localization

- `SharedResource.resx` (English) and `SharedResource.bn.resx` (Bangla) hold 175 interface strings;
  `SharedResource.cs` is the marker type. `tools/generate-resources.py` regenerates both files from
  a single source of truth.
- Views resolve text through a small `L["key"]` helper (`Views/_ViewImports.cshtml`), so a missing
  key is visible during development rather than silently blank.
- Culture is negotiated by `RequestLocalization` from `?culture=bn`, then the
  `.AspNetCore.Culture` cookie; `LanguageController` writes that cookie so the choice survives
  navigation (`/language/set?culture=bn&returnUrl=...`, with `returnUrl` validated as a local URL).
- Resource files are compiled as `Portfolio.Web.SharedResource(.bn).resources`, so
  `AddLocalization()` is called **without** `ResourcesPath`. Setting a path would make the localizer
  look for `Portfolio.Web.Resources.SharedResource` and every lookup would silently fall back to
  English (guarded by `BanglaCulture_TranslatesInterfaceChrome`).
- The default Razor encoder escapes non-ASCII as numeric entities; `Program.cs` registers
  `HtmlEncoder.Create(UnicodeRanges.All)` so Bangla is sent as UTF-8 instead of `&#x9AE;…`.
- Content translations are stored beside the English column (`Title` / `TitleBn`,
  `Body` / `BodyBn`, …). `ContentLocalization.Pick(english, bangla)` returns Bangla only when a
  translation exists, otherwise English — a documented, honest fallback.
- Dates use the current culture (`MMM yyyy`). The Bangla UI loads Noto Sans Bengali; both fonts are
  CSP-allowed from `fonts.googleapis.com` / `fonts.gstatic.com`.

## 7. Theming and motion

- `wwwroot/js/theme-init.js` runs before paint, reads `localStorage.theme`, falls back to
  `prefers-color-scheme`, and sets `data-theme` on `<html>`, so there is no flash of the wrong
  theme. `SiteSettings.DefaultTheme` seeds the initial behaviour for first-time visitors.
- `site.css` defines design tokens (`--surface`, `--text`, `--accent`, spacing, radii, shadows,
  transitions) for both themes; the same tokens drive the admin stylesheet.
- Animations are CSS transitions plus an `IntersectionObserver` reveal in `site.js`. Everything is
  inside `@media (prefers-reduced-motion: reduce)` guards, and no animation gates form submission or
  navigation.

## 8. Routing

```csharp
// Areas first, deliberately ordered ahead of attribute routes for link generation:
app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
   .Add(builder => { if (builder is RouteEndpointBuilder e) e.Order = -100; });

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
```

Public pages use absolute attribute routes for clean URLs (`[HttpGet("/projects")]`,
`[HttpGet("/research/{slug}")]`, `[HttpGet("/cv")]`, `[HttpGet("/media/{id:int}")]`).

**Why the area route is ordered:** a public action such as `ProjectsController.Index` is decorated
with `[HttpGet("/projects")]` and therefore shares controller and action names with
`Areas.Admin.Controllers.ProjectsController.Index`. Without an explicit order, URL generation prefers
the attribute route and a post-save `RedirectToAction(nameof(Index))` inside the admin area produced
`/projects` instead of `/Admin/Projects` — the administrator was thrown onto the public site after
every save. `AdminPost_RedirectsBackIntoTheAdminArea` in `AdminPanelTests` is the regression test;
it fails if the order is removed.

## 9. Assets

| File | Size | Purpose |
| --- | --- | --- |
| `wwwroot/css/site.css` | 48 KB | Design tokens, public layout, components, both themes, print and reduced-motion rules |
| `wwwroot/css/admin.css` | 25 KB | Admin layout and controls, built on the same tokens |
| `wwwroot/js/admin.js` | 4.4 KB | Sidebar, confirmation dialogs, media picker previews, unsaved-changes guard, submit lock |
| `wwwroot/js/site.js` | 4.9 KB | Theme toggle, mobile menu, reveal animations, dialog helpers, small progressive enhancements |
| `wwwroot/js/theme-init.js` | 0.7 KB | Pre-paint theme selection |
| `wwwroot/images/favicon.svg` | <1 KB | Favicon |

No bundler is used; static files are served with a content hash in the query string for cache
busting (`asp-append-version`), one week of client caching in production and `no-cache` in
development.

## 10. Testing seam

`public partial class Program;` at the end of `Program.cs` lets `WebApplicationFactory<Program>` boot
the real pipeline. The test host swaps SQL Server for SQLite in-memory, disables HTTPS redirection
and supplies deterministic seed credentials — see [testing.md](testing.md).
