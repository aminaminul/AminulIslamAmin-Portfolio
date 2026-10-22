using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Tests.Infrastructure;
using Portfolio.Web.Data;
using Portfolio.Web.Models.Entities;

namespace Portfolio.Tests;

[Collection(AppCollection.Name)]
public class ContactFormTests
{
    private readonly PortfolioAppFactory _factory;

    public ContactFormTests(PortfolioAppFactory factory) => _factory = factory;

    [Fact]
    public async Task ContactPage_RendersForVisitors()
    {
        var response = await _factory.CreateBrowserClient().GetAsync("/contact");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Contact", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task InvalidSubmission_ShowsValidationAndStoresNothing()
    {
        var marker = "invalid-" + TestData.Token();
        var client = _factory.CreateBrowserClient();

        var response = await _factory.PostFormAsync(client, "/contact", "/contact",
            new Dictionary<string, string?>
            {
                ["Name"] = "",
                ["Email"] = "not-an-email",
                ["Message"] = marker
            });

        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("field-error", html);
        Assert.Contains(marker, html); // the submitted message is preserved for correction
        Assert.Equal(0, await TestData.CountContactMessagesAsync(_factory, marker));
    }

    [Fact]
    public async Task ValidSubmission_IsStoredAndReportsEmailNotConfigured()
    {
        var marker = "valid-" + TestData.Token();
        var client = _factory.CreateBrowserClient();

        var response = await _factory.PostFormAsync(client, "/contact", "/contact",
            new Dictionary<string, string?>
            {
                ["Name"] = "Test Visitor",
                ["Email"] = "visitor@example.test",
                ["Subject"] = marker,
                ["Message"] = "Hello from the automated test.",
                ["Consent"] = "true"
            });

        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Email notifications are not configured", html);
        Assert.DoesNotContain("Your message was sent", html);

        using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.ContactMessages.SingleAsync(m => m.Subject == marker);
        Assert.Equal(EmailDeliveryState.NotConfigured, saved.EmailState);
        Assert.False(saved.IsRead);
        Assert.False(saved.IsArchived);
        // The client address is hashed, never stored raw. Test hosts have no remote address.
        Assert.True(string.IsNullOrEmpty(saved.IpHash) || saved.IpHash.Length <= 64);
    }

    [Fact]
    public async Task HoneypotSubmission_IsSilentlyDropped()
    {
        var marker = "honeypot-" + TestData.Token();
        var client = _factory.CreateBrowserClient();

        var response = await _factory.PostFormAsync(client, "/contact", "/contact",
            new Dictionary<string, string?>
            {
                ["Name"] = "Spam Bot",
                ["Email"] = "bot@example.test",
                ["Message"] = marker,
                ["Website"] = "https://spam.example"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, await TestData.CountContactMessagesAsync(_factory, marker));
    }

    [Fact]
    public async Task ContactFormClosed_RejectsSubmission()
    {
        var marker = "closed-" + TestData.Token();
        var client = _factory.CreateBrowserClient();

        // Obtain an antiforgery token while form is open
        var token = await PortfolioAppFactory.GetAntiforgeryTokenAsync(client, "/contact");

        await _factory.WithDbAsync(async db =>
        {
            var settings = await db.SiteSettings.FirstAsync();
            settings.ShowContactForm = false;
            await db.SaveChangesAsync();
        });
        _factory.GetService<Portfolio.Web.Services.ISiteSettingsService>().InvalidateCache();

        try
        {
            // Verify GET /contact hides form and shows closed message
            var getHtml = await client.GetStringAsync("/contact");
            Assert.Contains("contact form is currently closed", getHtml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<form", getHtml);

            // Verify direct POST submission is rejected
            var response = await client.PostAsync("/contact", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = "Any Visitor",
                ["Email"] = "visitor@example.test",
                ["Subject"] = marker,
                ["Message"] = "Message to closed form",
                ["Consent"] = "true",
                ["__RequestVerificationToken"] = token
            }));

            var html = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("contact form is currently closed", html, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await TestData.CountContactMessagesAsync(_factory, marker));
        }
        finally
        {
            await _factory.WithDbAsync(async db =>
            {
                var settings = await db.SiteSettings.FirstAsync();
                settings.ShowContactForm = true;
                await db.SaveChangesAsync();
            });
            _factory.GetService<Portfolio.Web.Services.ISiteSettingsService>().InvalidateCache();
        }
    }
}
