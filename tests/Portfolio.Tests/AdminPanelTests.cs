using System.Net;
using Microsoft.EntityFrameworkCore;
using Portfolio.Tests.Infrastructure;
using Portfolio.Web.Models.Entities;

namespace Portfolio.Tests;

[Collection(AppCollection.Name)]
public class AdminPanelTests
{
    private readonly PortfolioAppFactory _factory;

    public AdminPanelTests(PortfolioAppFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/Admin/Dashboard")]
    [InlineData("/Admin/Profile")]
    [InlineData("/Admin/Projects")]
    [InlineData("/Admin/Messages")]
    [InlineData("/Admin/Media")]
    public async Task AnonymousVisitor_IsRedirectedToLogin(string path)
    {
        var response = await _factory.CreateApiClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Account/Login", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task AnonymousVisitor_CannotSaveProfile()
    {
        var before = await TestData.GetProfileAsync(_factory);
        var token = await PortfolioAppFactory.GetAntiforgeryTokenAsync(
            _factory.CreateApiClient(), "/Admin/Account/Login");

        var response = await _factory.CreateApiClient().PostAsync("/Admin/Profile",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["FullName"] = "Anonymous Intruder",
                ["__RequestVerificationToken"] = token
            }));

        var after = await TestData.GetProfileAsync(_factory);
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual("Anonymous Intruder", after.FullName);
        Assert.Equal(before.FullName, after.FullName);
    }

    [Fact]
    public async Task PostWithoutAntiforgeryToken_IsRejected()
    {
        var client = await _factory.SignInAdminAsync();

        var response = await client.PostAsync("/Admin/Profile",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["FullName"] = "No token" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SignedInNonAdministrator_IsForbiddenFromAdminArea()
    {
        await _factory.EnsureStandardUserAsync();
        var client = await _factory.SignInStandardUserAsync();

        var response = await client.GetAsync("/Admin/Profile");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Account/AccessDenied", response.Headers.Location!.ToString());

        var denied = await client.GetAsync(response.Headers.Location!);
        var deniedHtml = await denied.Content.ReadAsStringAsync();
        Assert.Contains("Access denied", deniedHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SignedInNonAdministrator_CannotAccessChangePassword()
    {
        await _factory.EnsureStandardUserAsync();
        var client = await _factory.SignInStandardUserAsync();

        var response = await client.GetAsync("/Admin/Account/ChangePassword");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Account/AccessDenied", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShowsGenericMessage()
    {
        var client = _factory.CreateApiClient();
        var token = await PortfolioAppFactory.GetAntiforgeryTokenAsync(client, "/Admin/Account/Login");

        var response = await client.PostAsync("/Admin/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = PortfolioAppFactory.AdminEmail,
            ["Password"] = "definitely-not-the-password",
            ["__RequestVerificationToken"] = token
        }));

        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Email or password is incorrect.", html);
    }

    [Fact]
    public async Task AdminLogin_RedirectsToDashboard()
    {
        var client = _factory.CreateApiClient();
        var token = await PortfolioAppFactory.GetAntiforgeryTokenAsync(client, "/Admin/Account/Login");

        var response = await client.PostAsync("/Admin/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = PortfolioAppFactory.AdminEmail,
            ["Password"] = PortfolioAppFactory.AdminPassword,
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin", response.Headers.Location!.ToString());

        var dashboard = await client.GetAsync("/Admin/Dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
    }

    [Fact]
    public async Task AdminProfileSave_UpdatesPublicWebsite()
    {
        var token = TestData.Token();
        var displayName = $"Admin Edited {token}";
        var client = await _factory.SignInAdminAsync();

        var save = await _factory.PostFormAsync(client, "/Admin/Profile", "/Admin/Profile",
            new Dictionary<string, string?>
            {
                ["FullName"] = displayName,
                ["DisplayName"] = displayName,
                ["ProfessionalTitle"] = "Principal Test Engineer",
                ["ShortIntro"] = $"Public introduction {token}"
            });

        Assert.Equal(HttpStatusCode.Redirect, save.StatusCode);

        var persisted = await TestData.GetProfileAsync(_factory);
        Assert.Equal(displayName, persisted.FullName);
        Assert.Equal("Principal Test Engineer", persisted.ProfessionalTitle);

        var home = await _factory.CreateBrowserClient().GetStringAsync("/");
        Assert.Contains(displayName, home);
        Assert.Contains($"Public introduction {token}", home);
    }

    [Fact]
    public async Task InvalidProfileSubmission_DoesNotCorruptExistingRecord()
    {
        var client = await _factory.SignInAdminAsync();
        var before = await TestData.GetProfileAsync(_factory);

        // FullName is required, so the server must return the form instead of saving.
        var response = await _factory.PostFormAsync(client, "/Admin/Profile", "/Admin/Profile",
            new Dictionary<string, string?>
            {
                ["FullName"] = "",
                ["DisplayName"] = "Should not be saved"
            });

        var after = await TestData.GetProfileAsync(_factory);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("field-validation-error", html);
        Assert.Equal(before.FullName, after.FullName);
        Assert.NotEqual("Should not be saved", after.DisplayName);
    }

    [Fact]
    public async Task AdminPublishAndUnpublish_ControlsPublicVisibility()
    {
        var project = await TestData.AddProjectAsync(_factory, ContentStatus.Draft);
        var admin = await _factory.SignInAdminAsync();
        var publicClient = _factory.CreateApiClient();

        Assert.Equal(HttpStatusCode.NotFound, (await publicClient.GetAsync($"/projects/{project.Slug}")).StatusCode);

        var publish = await _factory.PostFormAsync(admin, $"/Admin/Projects/Publish?id={project.Id}",
            "/Admin/Projects", new Dictionary<string, string?>());
        Assert.Equal(HttpStatusCode.Redirect, publish.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await publicClient.GetAsync($"/projects/{project.Slug}")).StatusCode);

        var unpublish = await _factory.PostFormAsync(admin, $"/Admin/Projects/Unpublish?id={project.Id}",
            "/Admin/Projects", new Dictionary<string, string?>());
        Assert.Equal(HttpStatusCode.Redirect, unpublish.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publicClient.GetAsync($"/projects/{project.Slug}")).StatusCode);
    }

    [Fact]
    public async Task AdminPost_RedirectsBackIntoTheAdminArea()
    {
        // Regression: public actions such as [HttpGet("/projects")] share a controller and action
        // name with the admin area. Without the area route taking precedence during link
        // generation, a post-save redirect would send the administrator to the public page.
        var project = await TestData.AddProjectAsync(_factory, ContentStatus.Draft);
        var admin = await _factory.SignInAdminAsync();

        var publish = await _factory.PostFormAsync(admin, $"/Admin/Projects/Publish?id={project.Id}",
            "/Admin/Projects", new Dictionary<string, string?>());

        Assert.Equal(HttpStatusCode.Redirect, publish.StatusCode);
        var location = publish.Headers.Location!.ToString();
        Assert.StartsWith("/Admin/Projects", location);
    }

    [Fact]
    public async Task AdminDelete_RemovesProjectFromDatabaseAndPublicSite()
    {
        var project = await TestData.AddProjectAsync(_factory, ContentStatus.Published);
        var admin = await _factory.SignInAdminAsync();

        var delete = await _factory.PostFormAsync(admin, $"/Admin/Projects/Delete?id={project.Id}",
            "/Admin/Projects", new Dictionary<string, string?>());
        Assert.Equal(HttpStatusCode.Redirect, delete.StatusCode);

        var publicResponse = await _factory.CreateApiClient().GetAsync($"/projects/{project.Slug}");
        Assert.Equal(HttpStatusCode.NotFound, publicResponse.StatusCode);

        await _factory.WithDbAsync(async db =>
        {
            Assert.Null(await db.Projects.FindAsync(project.Id));
        });
    }

    [Fact]
    public async Task AdminCanCreateResearchAsDraftThenPublishIt()
    {
        var token = TestData.Token();
        var admin = await _factory.SignInAdminAsync();

        var create = await _factory.PostFormAsync(admin, "/Admin/Research/Create", "/Admin/Research/Create",
            new Dictionary<string, string?>
            {
                ["Title"] = $"Draft research {token}",
                ["Slug"] = $"draft-research-{token}",
                ["Abstract"] = "Created through the admin panel.",
                ["Status"] = ((int)ContentStatus.Draft).ToString()
            });

        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);

        Research? created = null;
        await _factory.WithDbAsync(async db =>
        {
            created = db.Research.OrderByDescending(r => r.Id).FirstOrDefault(r => r.Slug == $"draft-research-{token}");
        });
        Assert.NotNull(created);
        Assert.Equal(ContentStatus.Draft, created!.Status);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _factory.CreateApiClient().GetAsync($"/research/{created.Slug}")).StatusCode);

        var publish = await _factory.PostFormAsync(admin, $"/Admin/Research/Publish?id={created.Id}",
            "/Admin/Research", new Dictionary<string, string?>());
        Assert.Equal(HttpStatusCode.Redirect, publish.StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await _factory.CreateApiClient().GetAsync($"/research/{created.Slug}")).StatusCode);
    }

    [Fact]
    public async Task AdminCanCreateAndPublishCertification()
    {
        var token = TestData.Token();
        var admin = await _factory.SignInAdminAsync();

        var create = await _factory.PostFormAsync(admin, "/Admin/Certifications/Create", "/Admin/Certifications/Create",
            new Dictionary<string, string?>
            {
                ["Title"] = $"Cert {token}",
                ["Issuer"] = "Test Authority",
                ["Status"] = ((int)ContentStatus.Published).ToString()
            });

        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);

        await _factory.WithDbAsync(async db =>
        {
            var cert = await db.Certifications.FirstOrDefaultAsync(c => c.Title == $"Cert {token}");
            Assert.NotNull(cert);
            Assert.Equal("Test Authority", cert!.Issuer);
            Assert.Equal(ContentStatus.Published, cert.Status);
        });
    }

    [Fact]
    public async Task AdminCanCreateAndPublishExperience()
    {
        var token = TestData.Token();
        var admin = await _factory.SignInAdminAsync();

        var create = await _factory.PostFormAsync(admin, "/Admin/Experience/Create", "/Admin/Experience/Create",
            new Dictionary<string, string?>
            {
                ["JobTitle"] = $"Software Engineer {token}",
                ["Company"] = $"Acme Corp {token}",
                ["StartDate"] = "2023-01-01",
                ["Status"] = ((int)ContentStatus.Published).ToString()
            });

        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);

        await _factory.WithDbAsync(async db =>
        {
            var exp = await db.Experiences.FirstOrDefaultAsync(e => e.JobTitle == $"Software Engineer {token}");
            Assert.NotNull(exp);
            Assert.Equal($"Acme Corp {token}", exp!.Company);
            Assert.Equal(ContentStatus.Published, exp.Status);
        });
    }

    [Fact]
    public async Task AdminCanCreateAndPublishEducation()
    {
        var token = TestData.Token();
        var admin = await _factory.SignInAdminAsync();

        var create = await _factory.PostFormAsync(admin, "/Admin/Education/Create", "/Admin/Education/Create",
            new Dictionary<string, string?>
            {
                ["Institution"] = $"University of Science {token}",
                ["Qualification"] = "B.Sc. in Computer Science",
                ["Status"] = ((int)ContentStatus.Published).ToString()
            });

        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);

        await _factory.WithDbAsync(async db =>
        {
            var edu = await db.Educations.FirstOrDefaultAsync(e => e.Institution == $"University of Science {token}");
            Assert.NotNull(edu);
            Assert.Equal("B.Sc. in Computer Science", edu!.Qualification);
            Assert.Equal(ContentStatus.Published, edu.Status);
        });
    }

    [Fact]
    public async Task AdminCanCreateSkillCategoryAndSkill()
    {
        var token = TestData.Token();
        var admin = await _factory.SignInAdminAsync();

        var catCreate = await _factory.PostFormAsync(admin, "/Admin/Skills/CategoryCreate", "/Admin/Skills/Categories",
            new Dictionary<string, string?>
            {
                ["Name"] = $"Category {token}",
                ["IsVisible"] = "true"
            });

        Assert.Equal(HttpStatusCode.Redirect, catCreate.StatusCode);

        SkillCategory? category = null;
        await _factory.WithDbAsync(async db =>
        {
            category = await db.SkillCategories.FirstOrDefaultAsync(c => c.Name == $"Category {token}");
        });
        Assert.NotNull(category);

        var skillCreate = await _factory.PostFormAsync(admin, "/Admin/Skills/Create", "/Admin/Skills/Create",
            new Dictionary<string, string?>
            {
                ["Name"] = $"Skill {token}",
                ["CategoryId"] = category!.Id.ToString()
            });

        Assert.Equal(HttpStatusCode.Redirect, skillCreate.StatusCode);

        await _factory.WithDbAsync(async db =>
        {
            var skill = await db.Skills.FirstOrDefaultAsync(s => s.Name == $"Skill {token}");
            Assert.NotNull(skill);
            Assert.Equal(category.Id, skill!.CategoryId);
        });
    }

    [Fact]
    public async Task AdminMediaLifecycle_UploadServeRangeProtectAndDelete()
    {
        var token = TestData.Token();
        var admin = await _factory.SignInAdminAsync();
        var antToken = await PortfolioAppFactory.GetAntiforgeryTokenAsync(admin, "/Admin/Media");

        // 1. Upload an image
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(antToken), "__RequestVerificationToken");
        content.Add(new StringContent(((int)MediaKind.Image).ToString()), "Kind");
        content.Add(new StringContent($"Alt for {token}"), "AltText");
        var dummyPng = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        content.Add(new ByteArrayContent(dummyPng), "File", $"test-{token}.png");

        var uploadResponse = await admin.PostAsync("/Admin/Media/Upload", content);
        Assert.Equal(HttpStatusCode.Redirect, uploadResponse.StatusCode);

        MediaFile? media = null;
        await _factory.WithDbAsync(async db =>
        {
            media = await db.MediaFiles.FirstOrDefaultAsync(m => m.AltText == $"Alt for {token}");
        });
        Assert.NotNull(media);

        // 2. Public GET
        var publicClient = _factory.CreateApiClient();
        var serveResponse = await publicClient.GetAsync($"/media/{media!.Id}");
        Assert.Equal(HttpStatusCode.OK, serveResponse.StatusCode);
        Assert.Equal("image/png", serveResponse.Content.Headers.ContentType?.MediaType);

        // 3. Public GET with Range request
        var rangeReq = new HttpRequestMessage(HttpMethod.Get, $"/media/{media.Id}");
        rangeReq.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 3);
        var rangeResponse = await publicClient.SendAsync(rangeReq);
        Assert.Equal(HttpStatusCode.PartialContent, rangeResponse.StatusCode);

        // 4. Update Alt text
        var updateAlt = await _factory.PostFormAsync(admin, $"/Admin/Media/UpdateAlt?id={media.Id}", "/Admin/Media",
            new Dictionary<string, string?> { ["id"] = media.Id.ToString(), ["altText"] = $"Updated Alt {token}" });
        Assert.Equal(HttpStatusCode.Redirect, updateAlt.StatusCode);

        await _factory.WithDbAsync(async db =>
        {
            var reloaded = await db.MediaFiles.FindAsync(media.Id);
            Assert.Equal($"Updated Alt {token}", reloaded?.AltText);
        });

        // 5. In-use protection: attach media to a project
        int projectId = 0;
        await _factory.WithDbAsync(async db =>
        {
            var p = new Project
            {
                Title = $"Project using media {token}",
                Slug = $"proj-media-{token}",
                CoverImageMediaId = media.Id,
                Status = ContentStatus.Draft
            };
            db.Projects.Add(p);
            await db.SaveChangesAsync();
            projectId = p.Id;
        });

        // Attempt delete when in-use
        var deleteAttempt = await _factory.PostFormAsync(admin, $"/Admin/Media/Delete?id={media.Id}", "/Admin/Media",
            new Dictionary<string, string?>());
        Assert.Equal(HttpStatusCode.Redirect, deleteAttempt.StatusCode);

        // Media should STILL exist
        await _factory.WithDbAsync(async db =>
        {
            Assert.NotNull(await db.MediaFiles.FindAsync(media.Id));
            // Unlink media from project
            var p = await db.Projects.FindAsync(projectId);
            p!.CoverImageMediaId = null;
            await db.SaveChangesAsync();
        });

        // 6. Delete unused media
        var deleteSuccess = await _factory.PostFormAsync(admin, $"/Admin/Media/Delete?id={media.Id}", "/Admin/Media",
            new Dictionary<string, string?>());
        Assert.Equal(HttpStatusCode.Redirect, deleteSuccess.StatusCode);

        await _factory.WithDbAsync(async db =>
        {
            Assert.Null(await db.MediaFiles.FindAsync(media.Id));
        });
    }
}
