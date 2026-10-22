# Implementation Tracker: Portfolio CMS Improvements

This document tracks the progress, status, and verification of improvements implemented in the Portfolio CMS codebase, organized by priority batch.

## Status Legend
- `[Planned]` - Scheduled for implementation
- `[In Progress]` - Currently being worked on
- `[Implemented]` - Code changes made, pending verification
- `[Verified]` - Implemented and verified via tests/build
- `[Blocked]` - Dependent on external requirement or decision
- `[Deferred]` - Postponed to a later phase

---

## Batch 1: Immediate Bug Fixes & Core Stability (P0 / P1)

| Task ID | Item | Target File(s) | Status | Notes / Verification |
| :--- | :--- | :--- | :--- | :--- |
| **FIX-01** | Suppress valid form validation summary display | `src/Portfolio.Web/wwwroot/css/site.css` | `[Planned]` | Hides `.validation-summary-valid` on initial GET requests |
| **FIX-02** | Align testimonial avatar rendering on `/services` | `src/Portfolio.Web/Views/Home/Services.cshtml` | `[Planned]` | Renders `AvatarMediaId` matching `Home/Index.cshtml` |
| **FIX-03** | Fix non-Latin tag and tech de-duplication | `ProjectsController.cs`, `ResearchController.cs` | `[Planned]` | Checks `Name == name \|\| Slug == slug` |
| **FIX-04** | Clean up dead rule in `robots.txt` & media headers | `SitemapController.cs`, `MediaController.cs` | `[Planned]` | Remove `/media/documents` disallow, add `noindex` header on docs |
| **GIT-01** | Initialize git tracking for all workspace files | Root repository | `[Planned]` | Stage and commit all source files |

---

## Batch 2: Security & Architecture Hardening (P1 / P2)

| Task ID | Item | Target File(s) | Status | Notes / Verification |
| :--- | :--- | :--- | :--- | :--- |
| **SEC-01** | Bound `ForwardedHeadersOptions` known proxies | `src/Portfolio.Web/Program.cs` | `[Planned]` | Restrict known proxies to loopback sockets |
| **SEC-02** | Enforce `CookieSecurePolicy.Always` in production | `src/Portfolio.Web/Program.cs` | `[Planned]` | Identity and Antiforgery cookies |
| **DB-01** | Add database health check endpoint `/healthz` | `src/Portfolio.Web/Program.cs` | `[Planned]` | Built-in ASP.NET Core health check |
| **UX-01** | Render badge images for Certifications & Achievements | `src/Portfolio.Web/Views/Home/About.cshtml` | `[Planned]` | Display `ImageMediaId` when populated |
| **ARCH-01**| Optimize Admin Dashboard counts into grouped queries | `Areas/Admin/Controllers/DashboardController.cs` | `[Planned]` | Reduce network round-trips from 18 to 4 |

---

## Batch 3: High-Value Features & Test Expansion (P2)

| Task ID | Item | Target File(s) | Status | Notes / Verification |
| :--- | :--- | :--- | :--- | :--- |
| **FEAT-01**| Interactive technology filtering on `/projects` | `PortfolioQueryService.cs`, `ProjectsController.cs`, `Index.cshtml` | `[Planned]` | Filter projects by clicking technology chips |
| **FEAT-02**| Academic citation generator (BibTeX / APA modal) | `Views/Research/Details.cshtml` | `[Planned]` | Native HTML `<dialog>` with copy helper |
| **TEST-01**| Expand automated test coverage for Admin controllers | `tests/Portfolio.Tests/` | `[Planned]` | Integration tests for secondary admin controllers |

---

## Progress Log

### Batch 1 Execution
- **Started**: 2026-10-22
- **Completed**: In progress
