# Testing

## 1. Running the suite

```bash
dotnet test                       # Debug
dotnet test -c Release            # what the release check uses
dotnet test --filter "FullyQualifiedName~AdminPanelTests"
```

Latest result in this workspace:

```
Passed! - Failed: 0, Passed: 44, Skipped: 0, Total: 44  (Portfolio.Tests, net10.0)
```

`dotnet build -c Release` reports `0 Warning(s), 0 Error(s)`.

## 2. How the tests run

`Portfolio.Tests/Infrastructure/PortfolioAppFactory.cs` derives from
`WebApplicationFactory<Program>`; `public partial class Program;` at the bottom of `Program.cs` is the
seam. Each test collection boots the **real** pipeline — routing, localization, antiforgery,
Identity, Razor views, services, middleware — with three substitutions:

| Real | Test |
| --- | --- |
| SQL Server via `UseSqlServer` | SQLite in-memory, one open connection per factory |
| Administrator from user secrets | `AdminSeed` settings injected as test credentials |
| `App_Data/media`, `App_Data/keys` | a unique folder under the system temp directory, deleted on dispose |
| HTTPS redirection | disabled through the `Testing` environment |

Helpers: `CreateApiClient()` (no redirect following, so authorization can be asserted),
`CreateBrowserClient()` (follows redirects, for end-to-end page assertions), `SignInAdminAsync()`,
`SignInStandardUserAsync()`, `WithDbAsync()` (direct database assertions in a fresh scope),
`GetAntiforgeryTokenAsync()` / `PostFormAsync()` (form posts with a real token).

## 3. What is covered

### Public site — `PublicWebsiteTests`

| Test | Proves |
| --- | --- |
| `Home_RendersForAnonymousVisitor` | The home page renders from the database for an unauthenticated visitor. |
| `ProjectsPage_ListsPublishedAndHidesDrafts` | Listing shows only published projects. |
| `DraftProject_DetailPageReturnsNotFound` | A draft's slug is a 404, not a 403 or a leak. |
| `PublishedProject_DetailPageRendersContent` | A published project's detail page renders its content. |
| `ResearchDraft_IsHiddenFromListingDetailAndSitemap` | Draft research is absent from listing, detail and sitemap. |
| `Sitemap_OnlyContainsPublicEntries` | `sitemap.xml` contains published URLs only. |
| `RobotsTxt_DisallowsAdminAndLinksSitemap` | `robots.txt` disallows `/Admin` and points at the sitemap. |
| `UnknownRoute_Returns404` | The custom 404 path is wired. |
| `BanglaCulture_TranslatesInterfaceChrome` | `?culture=bn` renders Bangla interface text. |
| `Html_IncludesSecurityHeaders` | CSP, `X-Content-Type-Options` and `Referrer-Policy` are present. |
| `ServicesAndTestimonials_HideUnpublishedRecords` | Visibility flags are enforced server-side. |

### Admin panel — `AdminPanelTests`

| Test | Proves |
| --- | --- |
| `AnonymousVisitor_IsRedirectedToLogin` (×5 routes) | Every admin module requires authentication. |
| `AnonymousVisitor_CannotSaveProfile` | An unauthenticated POST cannot modify content. |
| `PostWithoutAntiforgeryToken_IsRejected` | Missing antiforgery token → 400, nothing saved. |
| `SignedInNonAdministrator_IsForbiddenFromAdminArea` | A signed-in non-admin gets Access Denied, not access. |
| `Login_WithWrongPassword_ShowsGenericMessage` | No account enumeration. |
| `AdminLogin_RedirectsToDashboard` | Successful sign-in reaches the dashboard. |
| `AdminProfileSave_UpdatesPublicWebsite` | **The core requirement:** saving the profile in the admin panel changes what visitors see (database row + rendered home page). |
| `InvalidProfileSubmission_DoesNotCorruptExistingRecord` | Validation failure returns the form with errors and leaves the stored record untouched. |
| `AdminPublishAndUnpublish_ControlsPublicVisibility` | Publish → detail 200; unpublish → detail 404. |
| `AdminPost_RedirectsBackIntoTheAdminArea` | Regression test for the area/attribute-route conflict (see [architecture.md](architecture.md#8-routing)); fails if the area route order is removed. |
| `AdminDelete_RemovesProjectFromDatabaseAndPublicSite` | Delete removes the row and the public page. |
| `AdminCanCreateResearchAsDraftThenPublishIt` | Full research workflow: create as draft → hidden → publish → visible. |

### Contact form — `ContactFormTests`

`ContactPage_RendersForVisitors`, `InvalidSubmission_ShowsValidationAndStoresNothing`,
`ValidSubmission_IsStoredAndReportsEmailNotConfigured` (proves the honest "saved but email not
configured" message and that the row is persisted), `HoneypotSubmission_IsSilentlyDropped`.

### Services — `ServiceTests`

- Markdown: `RawHtml_IsNotRendered`, `ExternalLinks_AreHardened`, `EmptyInput_ProducesNoMarkup`.
- Slugs: normalisation, fallback to source then prefix, path-traversal characters stripped,
  uniqueness suffixes.
- Localization: Bangla is used only when a translation exists.
- Media: valid image stored under a generated name, disallowed extension rejected, content/extension
  mismatch rejected, oversized file rejected, deletion refused while a file is in use.

## 4. Verified manually (not automated)

Because the test host uses SQLite, these are exercised against SQL Server LocalDB by hand and are
the first things to repeat after a change to the data layer:

| Check | Result |
| --- | --- |
| `dotnet ef database update` against LocalDB, then app start | Migrations applied; `InitialCreate` scheme created |
| Admin sign-in in a browser, **Profile** save → public home page | Name, title and intro appeared in the header, hero, footer and `<title>` |
| **New project** as draft → public `/projects` | Not listed |
| **Publish** from the admin list → `/projects` and `/projects/{slug}` | Listed, detail 200 |
| **Unpublish** → `/projects/{slug}` and `sitemap.xml` | 404, no sitemap entry |
| **Delete** → admin list | Row removed |
| Anonymous `/Admin` | 302 to `/Admin/Account/Login?ReturnUrl=%2FAdmin` |
| Anonymous `POST /Admin/Media/Upload` | 302 to the login page; nothing written |
| Authenticated upload of a real PNG | Stored under a generated name, served from `/media/1` as `image/png` with `nosniff` and `Cache-Control: public,max-age=86400` |
| Authenticated upload of a text file renamed `.png` | Rejected by the content-signature check; no file on disk, no DB row |
| Media delete | Row **and** file removed; `/media/1` returns 404 afterwards |
| `?culture=bn` on the public site | Navigation, skip link and CTAs render in Bangla (`lang="bn"`), no numeric entities in the HTML |
| Theme toggle | `data-theme` flips, `localStorage['portfolio-theme']` persists the choice across navigation |
| `dotnet publish -c Release` | `web.config`, assemblies, `wwwroot`, `bn/` satellite produced; `appsettings.Development.json` excluded; 0 warnings |
| Published output started with `ASPNETCORE_ENVIRONMENT=Production` | 200 on `/`, `/about`, `/projects`, `/research`, `/contact`, `/sitemap.xml`, `/robots.txt`, `/error/404`; 302 on `/Admin`; security headers present; no console or CSP errors in the browser |

## 5. Known gaps

- **No automated browser tests.** Razor output and HTTP behaviour are asserted; visual regressions,
  focus rings and animation timing are inspected manually. Adding Playwright is the natural next
  step.
- **SQL Server-specific behaviour** (migrations, index usage, collation) is not covered by the
  automated suite; only SQLite-compatible behaviour is.
- **SMTP delivery** is not tested against a live server. The disabled-email path *is* tested, and the
  configured path is exercised only by code review.
- **Upload limits at the host level** (IIS request filtering / plan quotas) can only be tested on the
  live host.
- **Bangla typography** is asserted at the markup level (`lang`, translated strings), not visually.
