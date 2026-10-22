# Setup and local development

This guide takes a fresh machine to a running site with an administrator account. It has been
exercised on Windows with .NET SDK 10.0.402 and SQL Server LocalDB; the same steps work with any
SQL Server 2019+ instance (LocalDB, Express, Developer or a hosted SQL Server).

## 1. Prerequisites

| Requirement | Notes |
| --- | --- |
| .NET SDK 10.0 | `dotnet --version` should print `10.0.x`. The project targets `net10.0`. |
| SQL Server | LocalDB (`(localdb)\MSSQLLocalDB`), SQL Server Express, Developer or a hosted instance. |
| `dotnet-ef` tool | `dotnet tool install --global dotnet-ef --version 10.*` |
| Any editor | Visual Studio 2022+, VS Code, Rider. |

Nothing else is required: the site does **not** need Node.js, npm, a bundler or a CSS framework.
The Markdig, MailKit and EF Core packages are restored by `dotnet restore`.

## 2. Restore and build

```bash
git clone <repository-url>
cd AminulIslamAmin-Portfolio
dotnet restore
dotnet build -c Release     # expect: 0 Warning(s), 0 Error(s)
```

## 3. Configure the database and administrator

Secrets must never live in the repository. For local development use user secrets:

```bash
cd src/Portfolio.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\MSSQLLocalDB;Database=PortfolioCms;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
dotnet user-secrets set "AdminSeed:Email" "you@example.com"
dotnet user-secrets set "AdminSeed:Password" "<long unique password>"
dotnet user-secrets set "AdminSeed:DisplayName" "Site Owner"
```

Alternatively use environment variables (this is how production hosts are configured):

```bash
export ConnectionStrings__DefaultConnection="Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True"
export AdminSeed__Email="you@example.com"
export AdminSeed__Password="<long unique password>"
```

The password policy is the one configured in `Program.cs`: at least 12 characters with upper case,
lower case, a digit and a symbol. `AdminSeed` only creates the account when it does not exist; the
password is never stored in configuration afterwards, so remove the values once the account exists
(change the password later from **Admin → Account & security**).

`appsettings.Development.json` already points at LocalDB and sets `Database:MigrateOnStartup=true`,
so a development machine applies migrations automatically at startup.

## 4. Create the schema

Automatic in development, explicit everywhere else:

```bash
# from the repository root
dotnet ef database update --project src/Portfolio.Web
```

In Development the app also runs `MigrateAsync()` on startup (`Database:MigrateOnStartup`), which is
what creates the LocalDB database on first run. In Production this flag defaults to `false` so that
schema changes are always deliberate — see [deployment.md](deployment.md).

## 5. Run

```bash
dotnet run --project src/Portfolio.Web
```

| URL | Purpose |
| --- | --- |
| `/` | Public portfolio |
| `/Admin` | Admin sign-in and dashboard (**the documented admin entry point**) |
| `/Admin/Account/Login` | Direct sign-in page |
| `/sitemap.xml`, `/robots.txt` | Generated SEO endpoints |
| `/media/{id}` | Media served from outside the web root |
| `/cv` | CV download (only while the resume section is enabled and a file is uploaded) |
| `/error/404`, `/error/500` | Status-code pages used by the pipeline |

On first start the log shows:

```
info: Portfolio.Web[0]  Applying pending EF Core migrations...
info: Portfolio.Web[0]  Seeded default site settings.
info: Portfolio.Web[0]  Created an empty profile row; fill it in from the admin panel.
info: Portfolio.Web[0]  Provisioned administrator you@example.com.
```

If you see `No administrator seed credentials supplied` the site runs fine but nobody can sign in —
set `AdminSeed__Email` / `AdminSeed__Password` and restart.

## 6. First five minutes in the admin panel

1. Sign in at `/Admin`.
2. **Profile** → full name, professional title, short introduction, profile photo.
3. **Website settings** → site name, tagline, default theme, availability text.
4. **Hero section** → headline, calls to action, which hero elements are visible.
5. **Projects → New project** → create one *draft*, check **Preview**, publish it, then open
   `/projects` in a private window to confirm it is now public.
6. **Contact information** → contact email; **Contact messages** lists what visitors send.
7. **Resume / CV** → upload the PDF; the `/cv` download button appears only while it is enabled.

The full walkthrough is in [admin-guide.md](admin-guide.md).

## 7. Where uploads go

Uploads are written to `App_Data/media` relative to the content root (configurable through
`Media:RootPath`). That folder is **outside** `wwwroot`, is never served directly, and is excluded
from git. Files are served by `/media/{id}` with a validated content type. In development the folder
is created on demand; on a host it must be persistent and writable — see
[deployment.md](deployment.md#6-media-and-persistence).

## 8. Configuration reference

`appsettings.json` ships with safe placeholders. Every value can be overridden with an environment
variable using `__` as the separator.

| Key | Default | Meaning |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | *(required)* | SQL Server connection string. Startup fails with a clear message when missing. |
| `Database:MigrateOnStartup` | `false` | Apply migrations at startup. `true` in `appsettings.Development.json`. |
| `Database:EnsureCreatedInTesting` | `true` | Used only by the test host (SQLite). |
| `Media:RootPath` | `App_Data/media` | Upload root, outside the web root. |
| `Media:MaxImageBytes` | `8388608` (8 MB) | Image upload limit. |
| `Media:MaxDocumentBytes` | `20971520` (20 MB) | Document upload limit. |
| `Media:PublicRequestPath` | `/media` | Public path used when rendering media URLs. |
| `Email:Host`, `Port`, `UseStartTls`, `UserName`, `Password`, `FromAddress`, `FromName`, `ToAddress` | empty | Optional SMTP. Email is *disabled* until host and sender are set. |
| `AdminSeed:Email`, `Password`, `DisplayName` | empty | One-time administrator provisioning. |
| `DataProtection:KeysPath` | `App_Data/keys` | Key ring location, persisted to disk. |
| `Site:PublicBaseUrl` | empty | Absolute base URL used in the sitemap and canonical links when behind a proxy. |

## 9. Troubleshooting

| Symptom | Cause / fix |
| --- | --- |
| `ConnectionStrings:DefaultConnection is required` | Configuration missing; set the secret or environment variable. |
| `The SQL Server database is not reachable` in the log | Wrong server/instance name, or SQL Server not running. |
| Can sign in but every admin page 403s | The user exists without the `Administrator` role; set `AdminSeed` for that email and restart, or re-provision. |
| `Administrator provisioning failed` | The password does not meet the policy. |
| Uploads disappear after a redeploy | The host wiped `App_Data`; configure persistent storage. |
| Bangla text renders with wrong glyphs | The Noto Sans Bengali web font is loaded from Google Fonts; a strict offline environment must host it locally. |
