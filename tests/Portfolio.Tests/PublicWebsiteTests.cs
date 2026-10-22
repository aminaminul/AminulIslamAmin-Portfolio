using System.Net;
using Portfolio.Tests.Infrastructure;
using Portfolio.Web.Models.Entities;

namespace Portfolio.Tests;

[Collection(AppCollection.Name)]
public class PublicWebsiteTests
{
    private readonly PortfolioAppFactory _factory;

    public PublicWebsiteTests(PortfolioAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Home_RendersForAnonymousVisitor()
    {
        var client = _factory.CreateBrowserClient();
        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<!DOCTYPE html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<main", html);
    }

    [Fact]
    public async Task ProjectsPage_ListsPublishedAndHidesDrafts()
    {
        var published = await TestData.AddProjectAsync(_factory, ContentStatus.Published);
        var draft = await TestData.AddProjectAsync(_factory, ContentStatus.Draft);

        var html = await _factory.CreateBrowserClient().GetStringAsync("/projects");

        Assert.Contains(published.Title, html);
        Assert.DoesNotContain(draft.Title, html);
    }

    [Fact]
    public async Task DraftProject_DetailPageReturnsNotFound()
    {
        var draft = await TestData.AddProjectAsync(_factory, ContentStatus.Draft);
        var client = _factory.CreateApiClient();

        var draftResponse = await client.GetAsync($"/projects/{draft.Slug}");
        var missingResponse = await client.GetAsync("/projects/does-not-exist-at-all");

        Assert.Equal(HttpStatusCode.NotFound, draftResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task PublishedProject_DetailPageRendersContent()
    {
        var project = await TestData.AddProjectAsync(_factory, ContentStatus.Published);

        var response = await _factory.CreateBrowserClient().GetAsync($"/projects/{project.Slug}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(project.Title, html);
        Assert.Contains(project.Summary!, html);
    }

    [Fact]
    public async Task ResearchDraft_IsHiddenFromListingDetailAndSitemap()
    {
        var draft = await TestData.AddResearchAsync(_factory, ContentStatus.Draft);
        var published = await TestData.AddResearchAsync(_factory, ContentStatus.Published);
        var client = _factory.CreateBrowserClient();

        var listing = await client.GetStringAsync("/research");
        var sitemap = await client.GetStringAsync("/sitemap.xml");
        var draftDetail = await _factory.CreateApiClient().GetAsync($"/research/{draft.Slug}");

        Assert.DoesNotContain(draft.Title, listing);
        Assert.Contains(published.Title, listing);
        Assert.DoesNotContain(draft.Slug, sitemap);
        Assert.Contains(published.Slug, sitemap);
        Assert.Equal(HttpStatusCode.NotFound, draftDetail.StatusCode);
    }

    [Fact]
    public async Task Sitemap_OnlyContainsPublicEntries()
    {
        var token = "sitemap" + TestData.Token();
        var project = await TestData.AddProjectAsync(_factory, ContentStatus.Published, token);
        var draft = await TestData.AddProjectAsync(_factory, ContentStatus.Draft, token + "d");

        var xml = await _factory.CreateBrowserClient().GetStringAsync("/sitemap.xml");

        Assert.Contains(project.Slug, xml);
        Assert.DoesNotContain(draft.Slug, xml);
        Assert.DoesNotContain("/Admin", xml);
        Assert.DoesNotContain("/media/", xml);
    }

    [Fact]
    public async Task RobotsTxt_DisallowsAdminAndLinksSitemap()
    {
        var text = await _factory.CreateBrowserClient().GetStringAsync("/robots.txt");

        Assert.Contains("Disallow: /Admin", text);
        Assert.Contains("Sitemap: https://portfolio.test/sitemap.xml", text);
    }

    [Fact]
    public async Task UnknownRoute_Returns404()
    {
        var response = await _factory.CreateApiClient().GetAsync("/this/page/does/not/exist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BanglaCulture_TranslatesInterfaceChrome()
    {
        using var client = _factory.CreateBrowserClient();

        var response = await client.GetAsync("/?culture=bn");
        var html = System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Asserts resource lookups, not seeded content: the skip link and the primary CTA are
        // rendered from SharedResource.bn.resx. A missing resource fallback would leave the
        // English text here, which is exactly the failure this test guards against.
        Assert.Contains("মূল কনটেন্টে যান", html);
        Assert.Contains("যোগাযোগ করুন", html);
        Assert.DoesNotContain(">Skip to content<", html);
        Assert.Contains("lang=\"bn\"", html);
    }

    [Fact]
    public async Task Html_IncludesSecurityHeaders()
    {
        var response = await _factory.CreateBrowserClient().GetAsync("/");

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Contains("nosniff", response.Headers.GetValues("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
    }

    [Fact]
    public async Task ServicesAndTestimonials_HideUnpublishedRecords()
    {
        var token = TestData.Token();
        var visible = new Service { Title = $"Visible service {token}", IsVisible = true };
        var hidden = new Service { Title = $"Hidden service {token}", IsVisible = false };
        var publishedTestimonial = new Testimonial
        {
            ClientName = "Client " + token,
            Quote = "Public quote " + token,
            Status = ContentStatus.Published,
            PublishedAt = DateTime.UtcNow
        };
        var draftTestimonial = new Testimonial
        {
            ClientName = "Draft client " + token,
            Quote = "Draft quote " + token,
            Status = ContentStatus.Draft
        };

        await _factory.WithDbAsync(async db =>
        {
            db.Services.AddRange(visible, hidden);
            db.Testimonials.AddRange(publishedTestimonial, draftTestimonial);
            await db.SaveChangesAsync();
        });

        var html = await _factory.CreateBrowserClient().GetStringAsync("/services");

        Assert.Contains(visible.Title, html);
        Assert.DoesNotContain(hidden.Title, html);
        Assert.Contains(publishedTestimonial.Quote, html);
        Assert.DoesNotContain(draftTestimonial.Quote, html);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthyOk()
    {
        var response = await _factory.CreateApiClient().GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", text);
    }

    [Fact]
    public async Task ProjectsPage_FiltersByTechnology()
    {
        var techSlug = "tech-" + TestData.Token();
        var p1 = await TestData.AddProjectAsync(_factory, ContentStatus.Published);
        var p2 = await TestData.AddProjectAsync(_factory, ContentStatus.Published);

        await _factory.WithDbAsync(async db =>
        {
            var tech = new Technology { Name = "Tech " + TestData.Token(), Slug = techSlug };
            db.Technologies.Add(tech);
            await db.SaveChangesAsync();

            db.ProjectTechnologies.Add(new ProjectTechnology { ProjectId = p1.Id, TechnologyId = tech.Id });
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateBrowserClient();
        var html = await client.GetStringAsync($"/projects?tech={techSlug}");

        Assert.Contains(p1.Title, html);
        Assert.DoesNotContain(p2.Title, html);
    }

    [Fact]
    public async Task ResearchDetails_RendersCitationModalAndButtons()
    {
        var research = await TestData.AddResearchAsync(_factory, ContentStatus.Published);
        var client = _factory.CreateBrowserClient();
        var html = await client.GetStringAsync($"/research/{research.Slug}");

        Assert.Contains("citation-modal", html);
        Assert.Contains("@article{", html);
    }
}
