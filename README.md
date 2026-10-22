# Portfolio CMS

A modern, high-performance personal portfolio website with a full content management system, built with
**C# / ASP.NET Core MVC (.NET 10)**, **Entity Framework Core 10**, **Microsoft SQL Server**, and
**ASP.NET Core Identity**.

Everything a visitor sees — the owner's name, title, bio, profile photo, skills, projects, academic research,
work experience, education, certifications, services, client testimonials, resume/CV, contact channels, navigation,
footer, and SEO metadata — is stored in SQL Server and managed through the authenticated admin panel.

---

## Contents

| Document | What it covers |
| --- | --- |
| [docs/IMPROVEMENT_PLAN.md](docs/IMPROVEMENT_PLAN.md) | Implementation tracker, verified bug fixes, hardening, and test evidence |
| [docs/setup.md](docs/setup.md) | Local development, prerequisites, first administrator, running the site |
| [docs/admin-guide.md](docs/admin-guide.md) | How to use the admin panel, content model, and publishing workflow |
| [docs/architecture.md](docs/architecture.md) | Project structure, request flow, services, localization, design system |
| [docs/database.md](docs/database.md) | Entities, relationships, indexes, migrations |
| [docs/security.md](docs/security.md) | Authentication, authorization, uploads, CSP, headers, secrets, hardening |
| [docs/deployment.md](docs/deployment.md) | Publishing to MonsterASP.NET and other IIS hosts, SQL Server, troubleshooting |
| [docs/testing.md](docs/testing.md) | Test suite (55 automated tests), what it proves, how to run it |

---

## Feature Summary

### Public Portfolio Site

- **Modern Responsive Design**: Clean, hand-crafted design system with light/dark theme switching (system preference default, persistent cookie/localStorage, zero flash of unstyled content).
- **Projects Showcase (`/projects`)**: Filterable project gallery with category chips, full-text search, and **interactive technology filtering** (`/projects?tech=...`) with removable filter chips.
- **Academic Research (`/research`)**: Full academic publication archive with authors, publication venue, DOI linking, PDF downloading, and an **accessible citation generator** supporting BibTeX and APA formats with one-click clipboard copying.
- **Career & Credentials (`/about`)**: Interactive timeline for work experience, academic background, achievements, and certification credentials with verified digital badge imagery.
- **Bilingual Interface**: Full English and Bangla (`bn`) localization across UI chrome and content records with automatic fallback.
- **SEO & Social Sharing**: Canonical URLs, Open Graph / Twitter Card meta tags, dynamically generated XML sitemap (`/sitemap.xml`) including only published records, crawler directives (`/robots.txt`), and search-engine-safe `noindex, nofollow` headers on error responses.
- **Health Check & Observability**: Uptime monitoring probe at **`/healthz`** verifying real-time database connectivity.
- **Accessibility (WCAG 2.2 AA)**: Semantic landmarks, skip-to-content link, accessible `:focus-visible` rings, screen-reader labels, dialog focus management, and automatic `@media (prefers-reduced-motion: reduce)` support.

### Admin CMS (`/Admin`)

- **Secure Authentication**: ASP.NET Core Identity with PBKDF2 password hashing, lockout protection after 5 failed attempts, sliding session expiration, and single-click "Sign out everywhere" session invalidation.
- **Optimized Dashboard**: Real-time content and message statistics with grouped SQL status queries to minimize database round-trips.
- **Granular Publishing Workflow**: Draft / Published / Archived states across all content. Unpublished drafts are strictly excluded from public pages, searches, and sitemaps.
- **Secure Media Management**: Uploads validated by signature and extension, stored safely outside the web root (`App_Data/media`), served through a controlled `/media/{id}` endpoint with HTTP range processing (`206 Partial Content`), and protected against accidental deletion when referenced by content.
- **Inbound Communication**: Contact form with honeypot spam traps, SHA-256 IP address hashing for privacy compliance, IP-based rate limiting (5 submissions per 10 minutes), and optional MailKit SMTP notifications.

---

## Quick Start

```bash
git clone <repository-url>
cd AminulIslamAmin-Portfolio

# 1. Configure user secrets (never commit these)
cd src/Portfolio.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\MSSQLLocalDB;Database=PortfolioCms;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
dotnet user-secrets set "AdminSeed:Email" "admin@example.com"
dotnet user-secrets set "AdminSeed:Password" "<a-strong-12+-character-password>"

# 2. Apply database migrations
cd ../..
dotnet ef database update --project src/Portfolio.Web

# 3. Run the application
dotnet run --project src/Portfolio.Web
```

The site will start on the URL printed in the console (e.g. `https://localhost:7xxx`).  
The CMS admin panel is at **`/Admin`** (also accessible from the footer and header navigation).

---

## Common Commands

```bash
dotnet restore                                             # Restore NuGet packages
dotnet build -c Release /warnaserror                       # Build with zero warnings
dotnet test                                                # Run the 55 automated tests
dotnet publish src/Portfolio.Web -c Release -o ./publish   # Generate production release
dotnet ef migrations add <MigrationName> --project src/Portfolio.Web
dotnet ef database update --project src/Portfolio.Web
```

---

## Technology Stack

| Concern | Choice |
| --- | --- |
| **Runtime & Framework** | .NET 10 (`net10.0`), ASP.NET Core MVC with Razor Views |
| **Data Access & ORM** | Microsoft SQL Server, Entity Framework Core 10 (`Microsoft.EntityFrameworkCore.SqlServer`) |
| **Authentication & Identity** | ASP.NET Core Identity with EF Core stores, role-based authorization (`Administrator`) |
| **Frontend Architecture** | Custom CSS3 design tokens, responsive CSS grid/flexbox, ~10 KB unobtrusive vanilla JS |
| **Security & Headers** | Strict Content Security Policy (`script-src 'self'`), AntiForgery validation, nosniff, frame-ancestors |
| **Icons & Media** | Inline SVG vector catalogue (`Views/Shared/_Icon.cshtml`), controlled streaming endpoint |
| **Markdown Processing** | Markdig with HTML sanitization and safe link target attributes |
| **Email Delivery** | MailKit & MimeKit (optional SMTP; messages persist in database regardless) |
| **Automated Testing** | xUnit, `Microsoft.AspNetCore.Mvc.Testing` with in-memory SQLite (55 tests) |

---

## Project Structure

```
PortfolioCMS.slnx
src/Portfolio.Web/
  Areas/Admin/            CMS controllers, Razor views, admin styling, and scripts
  Controllers/            Public endpoints (Home, Projects, Research, Contact, Media, Sitemap, Error)
  Data/                   AppDbContext, migrations, database initializer, design-time factory
  Infrastructure/         Health checks, paging, validation attributes, security headers, icon catalogue
  Models/Entities/        Persisted domain entities (Career, Content, Projects, Research, Settings, Skills)
  Models/ViewModels/      Strongly typed page, form, and admin models
  Resources/              SharedResource.resx (en) and SharedResource.bn.resx (bn)
  Services/               Portfolio queries, media storage, contact processor, markdown, slugs, email
  Views/                  Public Razor views, layouts, and partial components
  wwwroot/                css/, js/, images/ (client static assets)
tests/Portfolio.Tests/    Integration and functional test suite (55 tests)
docs/                     Architecture, setup, admin guide, database, security, deployment, testing
```

---

## Status & Verification

- **Compiler**: Builds cleanly in Release mode with **0 errors and 0 warnings** (`/warnaserror`).
- **Tests**: **55 passed, 0 failed, 0 skipped** across public endpoints, admin workflows, and security barriers.
- **Security & Integrity**: CSP-compliant, reverse proxy header hardening applied, and production cookies enforce `SecurePolicy.Always`.
- **Database**: Validated with EF Core migrations against SQL Server LocalDB and SQLite in-memory test fixtures.

---

## License

See [LICENSE](LICENSE).
