# Admin guide

Everything a visitor sees comes from the database and is edited here. You never need to touch source
code for ordinary content updates.

- **Sign in:** `https://your-domain/Admin` (also the shield link in the header and the "Admin" link in
  the footer). The sign-in page is `/Admin/Account/Login`.
- **Sign out:** the button at the bottom of the sidebar.

## 1. Dashboard

Real counts from SQL Server: published/draft projects, research, unread messages, testimonials
awaiting approval, skills, certifications, media files, about sections, experience and education
records. Below them: recent activity, common shortcuts, and a site-status card (site name,
availability, contact form state, whether a CV is uploaded).

If the profile is empty the dashboard shows a "Finish your profile" prompt — that is the fastest way
to make the site look real.

## 2. Profile (identity)

**Profile** feeds the header, hero, footer, page titles and social metadata.

| Field | Notes |
| --- | --- |
| Full name (required) | The name visitors see. |
| Display name | Optional shorter form; falls back to the full name. |
| Professional title (English / Bangla) | Shown under the name. |
| Location | Optional; shown with contact details. |
| Short introduction (English / Bangla) | Hero paragraph and default meta description. |
| Profile photo | Upload or pick from the media library; JPEG/PNG/WebP/GIF up to 8 MB. |
| Alternative text | Screen-reader description; defaults to your name. |

Saving is a real database write. On success you get a toast and the public site reflects the change
on the next request — no republish, no cache purge.

## 3. Hero section

Headline, subheadline, badge text, image and up to two calls to action, each with a label, a
destination and its own visibility toggle, plus toggles for the photo, social links and CV button and
an image-position choice (left/right). Turn off what you do not want — the layout adapts instead of
leaving an empty gap.

## 4. About me

Multiple blocks, each with a title, a Markdown body (English and optional Bangla), an optional image,
visibility and order. Markdown supports headings, lists, links, bold/italic and code; raw HTML is
escaped for safety, so pasted `<div>` markup will appear as text. **Preview** renders the block
through the public layout before you publish it.

## 5. Skills and categories

Categories group skills (Languages, Backend, Frontend, Databases, Tools, Concepts — whatever you
create). A skill has a name, optional description (both languages), category, optional icon name,
optional proficiency 0–100, visibility and order. Proficiency bars appear only where you enter a
value; leave it empty and no bar is drawn — nothing is invented on your behalf.

## 6. Projects

**Projects → New project**. Fields are grouped:

- **Basics:** title (English/Bangla), slug (auto-generated and de-duplicated when empty), category,
  short summary (both languages, used on cards and meta descriptions).
- **Case study:** overview, problem statement, solution, key features — all Markdown.
- **Links and timeline:** repository URL, live-demo URL, start date, completion date (empty = ongoing),
  project stage (Planned / InProgress / Completed / Maintained / Archived). Only mark something
  Completed when it is.
- **Media:** cover image plus up to ten gallery images per upload; reorder, set the cover and remove
  images from **Images** after saving.
- **Technology stack:** comma-separated; new technologies are added to the shared catalogue.
- **Publishing and SEO:** status (Draft / Published / Archived), display order, SEO title and
  description, and "Feature this project" (shown first and on the home page).

The list screen filters by status and category, searches by title or slug, and offers Publish,
Unpublish, Archive, Feature, Move up/down and Delete — all real POST operations with confirmation.

**Drafts are invisible to visitors:** not in the listing, the home page, search, related projects, the
sitemap, or the detail URL (which returns 404).

## 7. Research

Research is a first-class module with its own listing and detail pages.

- **Structure:** title, slug, abstract (both languages), introduction, methodology, tools, datasets,
  experiments, findings, discussion, limitations, conclusion, future work, references — each stored as
  its own field so the public page renders them as labelled sections rather than one wall of text.
- **Metadata:** category, keywords, start date, publication date, publication venue, journal, DOI,
  external URL, cover image, PDF, related projects, authors, tags, featured flag, display order,
  SEO fields and status.
- **Authors and tags** are ordered lists you manage on the same screen; **related projects** link to
  projects you already have.
- The editor marks required fields (title, slug) and leaves everything else optional. Do not fill in
  a DOI, venue or author that is not real.

Public listing supports search by title/keyword, category and year filters, and pagination. Only
published records appear; drafts and archived records are excluded from listings, details and the
sitemap.

## 8. Experience and education

Timelines with title/institution, organisation, employment type, location, start and end dates, a
"current" flag, responsibilities/achievements (Markdown), technologies, order and status. They are
rendered on the about page and summarised on the home page. If you have no verified entries, leave
them empty — the sections render a clean empty state instead of invented history.

## 9. Certifications, achievements, services, testimonials

| Module | Highlights |
| --- | --- |
| Certifications | Title, issuer (+ URL), issue/expiry date, credential ID, verification URL, image, description, order, status. |
| Achievements | Title, description, date, issuing organisation, image, external link, visibility, order. |
| Services | Title, description, optional icon, optional starting price + currency (leave empty to show no price), CTA label and destination, visibility, order. |
| Testimonials | Client name, role, organisation, quote (both languages), avatar, date, source URL. Publishing **is** approval: nothing appears until you set it to Published, which is the moderation step. |

## 10. Resume / CV

**Resume / CV** uploads a PDF (up to 20 MB), sets the button label (both languages), an optional
headline, and whether the section is shown at all. The `/cv` URL downloads the current file and
returns 404 while the section is disabled or no file exists — visitors never get a broken link.
Replace the file at any time; the old media record can then be deleted from the media library if
nothing else uses it.

## 11. Media library

Upload images and PDFs, preview them, copy their ids and delete them. Every file is stored under a
generated name outside the web root and served through `/media/{id}`. Deletion is refused while a
content record still references the file, and the UI tells you where it is used. Upload validation:
extension allow-list, size limit and content-signature check (a renamed file is rejected).

## 12. Contact information and messages

**Contact information** controls the email, phone, location, contact intro (both languages), the
"available for work" indicator and text, the form recipient and whether the form is open.

**Messages** lists submissions stored in SQL Server with search, read/unread and archived filters,
a detail view, mark-as-read and archive/delete. Each message records whether an email notification
was sent — if SMTP is not configured the message says so, and the visitor was told the truth as well.

The form itself is rate limited (5 submissions per 10 minutes per IP), has a hidden honeypot field,
requires an antiforgery token, and validates every field on the server.

## 13. Social links, navigation and footer

- **Social links:** platform, URL, icon name, whether the link appears in the hero and/or footer,
  visibility and order. Invalid URLs are rejected, and empty links are never rendered as buttons.
- **Navigation & footer:** every menu item — label (both languages), URL, whether it shows in the
  footer, whether it opens in a new tab, visibility and order. Items whose target section is disabled
  are not shown, so visitors cannot reach a dead page.
- **Footer:** footer description (both languages), copyright text (the year is generated), and the
  availability line.

## 14. SEO and website settings

- **SEO settings:** default title, description, keywords, Open Graph image and extra `robots.txt`
  lines. Each project/research record can override title and description.
- **Website settings:** site name (both languages), tagline (both languages), logo text or logo
  image, default theme (`system` / `light` / `dark`), accent colour and availability.

`/sitemap.xml` and `/robots.txt` are generated from published content only.

## 15. Account and security

Change the password (current password required, new password must meet the 12-character policy) and
review the sign-in details. A password change invalidates other sessions at the next security-stamp
check (within 15 minutes).

## 16. Themes and language as a visitor

The toggle in the header switches light/dark; the choice is remembered and defaults to the operating
system preference (or your configured default theme). The `EN / বাংলা` switch stores the language in a
cookie and keeps it across pages. Bangla interface text is complete; content falls back to English
wherever you have not entered a Bangla translation.

## 17. Publishing checklist

1. Profile and hero filled in.
2. Website settings: site name, tagline, contact email.
3. At least one project and, if applicable, one research record published.
4. CV uploaded (or the resume section switched off).
5. Social links point where you intend.
6. Open the public site in a private window and check home, projects, research and contact.
7. Submit the contact form once and confirm the message arrives in **Messages**.
