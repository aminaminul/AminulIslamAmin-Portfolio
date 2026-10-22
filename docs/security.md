# Security

This document describes what the application enforces, where the control lives, and what remains
the operator's responsibility.

## 1. Authentication

| Control | Implementation |
| --- | --- |
| Identity provider | ASP.NET Core Identity with EF Core stores (`ApplicationUser`, `IdentityRole`) |
| Password hashing | Identity's default `PasswordHasher` (PBKDF2 with the framework's current iteration count and a per-user salt). No custom hashing anywhere. |
| Password policy | Minimum 12 characters, upper + lower + digit + symbol |
| Lockout | 5 failed attempts → 15 minute lockout for all users |
| Cookie | `Portfolio.AdminAuth`, `HttpOnly`, `SameSite=Lax`, `Secure` when the request is HTTPS, 8-hour lifetime with sliding expiration |
| Security stamp | Revalidated every 15 minutes, so a password change or role removal invalidates live sessions |
| Session invalidation | `SignOutAsync` on logout; "sign out everywhere" is achieved by changing the password |
| Anti-forgery | Global `AutoValidateAntiforgeryTokenAttribute` on all controllers plus explicit `[ValidateAntiForgeryToken]` on admin POSTs; cookie `Portfolio.Antiforgery`, `SameSite=Strict` |
| Registration | There is **no** public registration endpoint. Accounts are provisioned from configuration (`AdminSeed`) or created by an existing administrator. |
| Error messages | Sign-in failures always say "Email or password is incorrect." — no account enumeration, no lockout disclosure |

The initial administrator never ships with a default password: `AdminSeed:Email` and
`AdminSeed:Password` are empty in `appsettings.json`, the app logs a warning when they are absent,
and the credentials exist only in user secrets or host environment variables. Remove them after the
first successful provision.

## 2. Authorization

- `AdminControllerBase` carries `[Area("Admin")]` and `[Authorize(Roles = "Administrator")]`; every
  admin controller inherits it. A new admin screen is therefore protected by default.
- Anonymous requests to `/Admin/**` are redirected to `/Admin/Account/Login?ReturnUrl=…`; a signed-in
  user without the role is redirected to `/Admin/Account/AccessDenied` (403 semantics) — verified by
  `SignedInNonAdministrator_IsForbiddenFromAdminArea`.
- Media upload, delete and reorder actions live inside the admin area, so they require the role as
  well.
- Public controllers only ever query published/visible rows; there is no public write endpoint other
  than `POST /contact`.
- UI hiding is never the control: every mutation re-checks authorization on the server, and every
  mutation is a POST with an antiforgery token.

## 3. Input handling

- Server-side validation for every form (DataAnnotations on dedicated ViewModels, plus
  `Infrastructure/ValidationAttributes.cs` for URLs, slugs and safe internal redirects).
  Client-side validation is a convenience only.
- EF Core parameterises all SQL; no string concatenation and no raw SQL in the application.
- Razor encodes output by default. `Html.Raw` is used **only** for `IMarkdownService.ToHtml` output.
- Markdown is rendered with Markdig's `DisableHtml()`, so raw HTML in a body field is escaped and
  cannot execute; links are hardened with `rel="nofollow noopener"` and `target="_blank"` only for
  absolute URLs (`RawHtml_IsNotRendered`, `ExternalLinks_AreHardened`).
- Redirects accept only local URLs (`Url.IsLocalUrl`) — the language switcher validates `returnUrl`.
- External URLs supplied by the administrator (repository, live demo, verification, social) are
  validated as absolute `http`/`https` URLs before rendering.
- Uploaded file names are never used as paths: storage names are `Guid.NewGuid():N` + validated
  extension, and the original name is kept only as a display label.
- String lengths are bounded in both the ViewModel and the database mapping, so an oversized value
  is rejected instead of silently truncated.

## 4. File uploads

`MediaService.SaveAsync` enforces, in order:

1. Non-empty file.
2. Extension allow-list: images `.jpg .jpeg .png .webp .gif`, documents `.pdf` (configurable).
3. Size limit: 8 MB images, 20 MB documents (configurable, also enforced by
   `[RequestSizeLimit]` on the actions).
4. **Content sniffing:** the first bytes are compared with the expected signature for the extension;
   a renamed executable is rejected with "The file content does not match its extension."
5. Write to a `.tmp` file, then atomic move — an interrupted upload leaves no half-written asset.
6. Metadata persisted; the file lives at `App_Data/media/{images|documents}/{guid}.{ext}`.

Serving: `/media/{id}` (`MediaController`) resolves the record, refuses to answer when the file has
disappeared (404 rather than a broken image), sends `X-Content-Type-Options: nosniff`, the stored
content type, range support and `Cache-Control: public,max-age=86400`. Nothing under `App_Data` is
reachable by path — there is no static-file mapping to it, and `wwwroot` contains no uploads.

Documents are always sent as attachments with a sanitised `Content-Disposition` file name, so a PDF
cannot be rendered as HTML by the browser.

Deletion is refused while a record still references the file (`MediaInUseException` reports the
usages), which prevents dangling image references.

## 5. Contact form abuse control

- Antiforgery token required.
- Fixed-window rate limit of **5 submissions per 10 minutes per client IP**
  (`EnableRateLimiting("contact")`, `429` on excess). Client IP comes from
  `ForwardedHeaders` + `KnownNetworks`/`KnownProxies` cleared so a proxy can pass the real address.
- Hidden honeypot field (`Website`): filled by bots, silently accepted and discarded
  (`HoneypotSubmission_IsSilentlyDropped`). No message is stored.
- Validation: name/email/subject/message length limits and a valid email address.
- The visitor's IP is only stored as a salted SHA-256 hash (`IpHash`) for abuse triage.
- The response is honest: when SMTP is not configured the visitor sees "Your message was saved.
  Email notifications are not configured on this site yet…" instead of a false delivery claim.

## 6. Transport and response headers

`UseSecurityHeaders()` sets, for every response:

```
X-Content-Type-Options: nosniff
X-Frame-Options: SAMEORIGIN
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: camera=(), microphone=(), geolocation=()
Cross-Origin-Opener-Policy: same-origin
Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'
  https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data:;
  connect-src 'self'; form-action 'self'; frame-ancestors 'self'; base-uri 'self'; object-src 'none'
```

`script-src 'self'` means **no inline scripts** — the theme bootstrap is an external file
(`theme-init.js`), which is why there is no theme flash without inline code. `style-src` allows
`'unsafe-inline'` only because Razor emits a few dynamic style values; no user-supplied CSS is
rendered.

HTTPS: `UseHsts()` and `UseHttpsRedirection()` in production, `UseForwardedHeaders()` so TLS
terminating at the host's proxy still produces correct scheme and client IP.

## 7. Secrets and data protection

| Secret | Where it belongs |
| --- | --- |
| SQL Server password | `ConnectionStrings__DefaultConnection` environment variable (or user secrets locally) |
| SMTP password | `Email__Password` environment variable |
| Administrator bootstrap password | `AdminSeed__Password`, removed after provisioning |
| Data-protection key ring | `DataProtection:KeysPath` (default `App_Data/keys`), persisted to disk so cookies survive restarts and redeploys |

Nothing secret is committed: `appsettings.json` contains empty strings, `.gitignore` excludes
`App_Data/`, and no secret is written to logs. Logging uses structured messages with ids and counts;
contact messages are logged by id only, never with their body, and the password/token fields are
never logged.

## 8. Error handling

- Development shows the developer exception page; production uses `/error/500`
  (`UseExceptionHandler`) and `UseStatusCodePagesWithReExecute("/error/{0}")`.
- The production error page shows a neutral message and a correlation-friendly status code — no
  stack trace, connection string, file path or SQL text.
- Unknown routes and unpublished slugs both return 404, so draft existence is not disclosed.

## 9. What remains the operator's job

1. Serve the site over HTTPS only (host setting / certificate), and set
   `ASPNETCORE_ENVIRONMENT=Production` so development pages and verbose errors are not exposed.
2. Use a least-privilege database login (the hosting control panel's database user, not `sa`).
3. Rotate the administrator password immediately after first sign-in and delete `AdminSeed` values.
4. Keep .NET runtime and NuGet packages patched (`dotnet list package --vulnerable`).
5. Back up the database **and** `App_Data/media` — the CMS cannot restore deleted content.
6. If you add `accounts.google.com`-style third-party scripts, update the CSP deliberately.
