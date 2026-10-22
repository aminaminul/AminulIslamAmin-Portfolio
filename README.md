# Portfolio CMS

A database-driven personal portfolio website with a full content management system, built with
**C# / ASP.NET Core MVC (.NET 10)**, **Entity Framework Core**, **Microsoft SQL Server** and
**ASP.NET Core Identity**.

Everything a visitor sees — the owner's name, title, photo, biography, skills, projects, research,
experience, education, certifications, services, testimonials, CV, contact details, navigation,
footer and SEO metadata — is stored in SQL Server and edited from the admin panel. No source change
is required for ordinary content updates.

---

## Contents

| Document | What it covers |
| --- | --- |
| [docs/setup.md](docs/setup.md) | Local development, prerequisites, first administrator, running the site |
| [docs/admin-guide.md](docs/admin-guide.md) | How to use the admin panel, content model and publishing workflow |
| [docs/architecture.md](docs/architecture.md) | Project structure, request flow, services, localization, design system |
| [docs/database.md](docs/database.md) | Entities, relationships, indexes, migrations |
| [docs/security.md](docs/security.md) | Authentication, authorization, uploads, headers, secrets, hardening |
| [docs/deployment.md](docs/deployment.md) | Publishing to MonsterASP.NET and other IIS hosts, SQL Server, troubleshooting |
| [docs/testing.md](docs/testing.md) | Test suite, what it proves, how to run it |

---

## Feature summary

**Public site**

- Responsive single-design-system portfolio: home/hero, about, skills, projects (list + detail),
  research (list + detail with structured sections), services, experience, education,
  certifications, achievements, testimonials, CV download, contact and a configurable footer.
- Light and dark themes with a system-preference default, no flash of the wrong theme, and a
  persisted choice.
- English and Bangla interface with Bangla content fields per record and a documented English
  fallback.
- SEO: per-page titles and descriptions, canonical URLs, Open Graph tags, XML sitemap (published
  content only), `robots.txt`, clean slugs, custom 404/500 pages.
- Accessibility: semantic landmarks, skip link, visible focus, labelled forms, keyboard-operable
  navigation and dialogs, `prefers-reduced-motion` support.
- Honest empty states. Nothing is invented: an unconfigured section renders a clean message.

**Admin panel (`/Admin`)**

- ASP.NET Core Identity sign-in, lockout, role-based authorization, no public registration.
- Dashboard with real counts and recent activity from SQL Server.
- Management screens for profile, hero, about sections, skills and categories, projects (with
  gallery and technology tags), experience, education, research (with authors, tags, documents,
  related projects), certifications, achievements, services, testimonials, resume/CV, contact
  information, social links, navigation, footer, SEO, media library, contact messages and account
  security.
- Draft / published / archived workflow for every publishable entity. Drafts never reach public
  pages, listings, search, the sitemap or detail URLs.
- Server-side validation, antiforgery protection on every state-changing request, upload
  validation, ordering controls and confirmation dialogs.
- Media library backed by database metadata plus files stored **outside** the web root and served
  through a controlled `/media/{id}` endpoint.

---

## Quick start

```bash
git clone <repository-url>
cd AminulIslamAmin-Portfolio

# 1. Configure secrets (never commit these)
cd src/Portfolio.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\MSSQLLocalDB;Database=PortfolioCms;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
dotnet user-secrets set "AdminSeed:Email" "you@example.com"
dotnet user-secrets set "AdminSeed:Password" "<a long unique password>"

# 2. Create the database
cd ../..
dotnet ef database update --project src/Portfolio.Web

# 3. Run
dotnet run --project src/Portfolio.Web
```

The site starts on the URL printed in the console (for example `https://localhost:7xxx`).
The admin panel is at **`/Admin`** (also linked from the footer and the header shield icon).

Full instructions, including how to provision the administrator on a host where user secrets are
unavailable, are in [docs/setup.md](docs/setup.md).

---

## Common commands

```bash
dotnet restore                                   # restore packages
dotnet build -c Release                          # build everything (0 warnings)
dotnet test                                      # run the automated test suite
dotnet publish src/Portfolio.Web -c Release -o ./publish   # deployment output
dotnet ef migrations add <Name> --project src/Portfolio.Web
dotnet ef database update --project src/Portfolio.Web
```

---

## Technology

| Concern | Choice |
| --- | --- |
| Runtime | .NET 10 (`net10.0`), ASP.NET Core MVC with Razor Views |
| Database | Microsoft SQL Server, EF Core 10 (`Microsoft.EntityFrameworkCore.SqlServer`) |
| Identity | ASP.NET Core Identity with EF Core stores, `Administrator` role |
| Frontend | Hand-authored CSS design system, no CSS framework dependency, ~10 KB of vanilla JS |
| Icons | Inline SVG sprite generated by `Views/Shared/_Icon.cshtml` (Lucide-derived paths) |
| Markdown | Markdig with raw HTML disabled and link hardening |
| Email | MailKit (optional; the contact form stores messages in SQL Server regardless) |
| Tests | xUnit + `Microsoft.AspNetCore.Mvc.Testing` against SQLite in-memory |

---

## Project layout

```
PortfolioCMS.slnx
src/Portfolio.Web/
  Controllers/            public request handling (Home, Projects, Research, Contact, Media, Sitemap)
  Areas/Admin/            protected CMS: controllers, Razor views, layouts, admin JS/CSS
  Models/Entities/        persisted entities
  Models/ViewModels/      strongly typed page and form models
  Data/                   AppDbContext, migrations, startup initializer, design-time factory
  Services/               settings, queries, media, contact, sitemap, markdown, slugs, email
  Infrastructure/         paging, validation attributes, security headers, icon catalogue
  Resources/              SharedResource.resx (en) and SharedResource.bn.resx (bn)
  Views/                  public Razor views and shared partials
  wwwroot/                css/, js/, images/
tests/Portfolio.Tests/    integration and unit tests
docs/                     architecture, setup, database, security, deployment, testing, admin guide
tools/generate-resources.py  helper that writes the .resx files from one source of truth
```

---

## Deployment in one paragraph

Publish with `dotnet publish src/Portfolio.Web -c Release`, then upload the output (Web Deploy,
FTP/SFTP or ZIP) to an IIS host with the ASP.NET Core Hosting Bundle for the matching .NET version.
Set `ASPNETCORE_ENVIRONMENT=Production`, provide `ConnectionStrings__DefaultConnection`,
`AdminSeed__Email` and `AdminSeed__Password` as environment variables, apply migrations and open
`/Admin`. Media is stored under `App_Data/media`, outside the web root, so that folder must persist
across deployments. See [docs/deployment.md](docs/deployment.md) for the verified MonsterASP.NET
walkthrough and troubleshooting.

---

## Status

The application in this repository builds in Release with no warnings, the automated suite passes,
and the end-to-end flows (admin sign-in, content save, publish/unpublish, public rendering,
contact form, media upload) have been exercised against a real SQL Server LocalDB database.
**It has not been deployed to a live host**; the deployment guide describes exactly what remains for
you to do with your own hosting credentials.

## License

See [LICENSE](LICENSE).
