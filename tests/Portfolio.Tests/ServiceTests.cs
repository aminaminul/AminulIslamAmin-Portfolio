using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Portfolio.Tests.Infrastructure;
using Portfolio.Web.Data;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.Options;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class MarkdownTests
{
    private readonly MarkdownService _markdown = new();

    [Fact]
    public void RawHtml_IsNotRendered()
    {
        var html = _markdown.ToHtml("<script>alert('xss')</script>\n\n**bold**");

        Assert.DoesNotContain("<script", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("<strong>bold</strong>", html);
    }

    [Fact]
    public void ExternalLinks_AreHardened()
    {
        var html = _markdown.ToHtml("[example](https://example.com/page)");

        Assert.Contains("rel=\"nofollow noopener\"", html);
        Assert.Contains("target=\"_blank\"", html);
    }

    [Fact]
    public void EmptyInput_ProducesNoMarkup()
    {
        Assert.Equal(string.Empty, _markdown.ToHtml(null));
        Assert.Equal(string.Empty, _markdown.ToHtml("   "));
    }
}

public class SlugTests
{
    private readonly SlugService _slugs = new();

    [Fact]
    public void Create_NormalizesCharacters()
    {
        Assert.Equal("hello-world", _slugs.Create("  Hello, World!  ", null, "project"));
        Assert.Equal("cafe-au-lait", _slugs.Create("Café au lait", null, "project"));
        Assert.Equal("a-b-c", _slugs.Create("a---b___c", null, "project"));
    }

    [Fact]
    public void Create_FallsBackToSourceThenPrefix()
    {
        Assert.Equal("my-title", _slugs.Create(null, "My Title", "project"));

        var generated = _slugs.Create(null, null, "project");
        Assert.StartsWith("project-", generated);
    }

    [Fact]
    public void Create_StripsPathTraversalCharacters()
    {
        var slug = _slugs.Create("../../etc/passwd", null, "project");

        Assert.DoesNotContain('/', slug);
        Assert.DoesNotContain("..", slug);
    }

    [Fact]
    public async Task EnsureUnique_AppendsSuffixes()
    {
        var taken = new HashSet<string> { "my-post", "my-post-2" };

        var unique = await _slugs.EnsureUniqueAsync("my-post", s => Task.FromResult(taken.Contains(s)));

        Assert.Equal("my-post-3", unique);
    }
}

public class LocalizationFallbackTests
{
    [Fact]
    public void Pick_UsesBanglaOnlyWhenTranslated()
    {
        var original = Thread.CurrentThread.CurrentUICulture;
        try
        {
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("bn");

            Assert.Equal("বাংলা", "English".Pick("বাংলা"));
            Assert.Equal("English", "English".Pick(null));
            Assert.Equal("English", "English".Pick("  "));
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = original;
        }
    }
}

[Collection(AppCollection.Name)]
public class MediaServiceTests
{
    private readonly PortfolioAppFactory _factory;

    public MediaServiceTests(PortfolioAppFactory factory) => _factory = factory;

    private (MediaService Service, AppDbContext Db) CreateService(long maxImageBytes = 8 * 1024 * 1024)
    {
        var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var options = Options.Create(new MediaOptions
        {
            RootPath = Path.Combine(_factory.MediaRoot, "media-" + Guid.NewGuid().ToString("N")),
            MaxImageBytes = maxImageBytes
        });
        var service = new MediaService(db, options, env, NullLogger<MediaService>.Instance);
        return (service, db);
    }

    private static FormFile PngFile(string fileName = "photo.png", int extraBytes = 16) =>
        new(new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. new byte[extraBytes]]),
            0, 8 + extraBytes, "upload", fileName);

    [Fact]
    public async Task ValidImage_IsStoredWithGeneratedName()
    {
        var (service, _) = CreateService();

        var media = await service.SaveAsync(PngFile("../../evil name.png"), MediaKind.Image, "Alt", "tester");

        Assert.Equal(".png", media.Extension);
        Assert.Equal("image/png", media.ContentType);
        Assert.DoesNotContain("..", media.StorageName);
        Assert.DoesNotContain("evil", media.StorageName);
        Assert.True(File.Exists(Path.Combine(service.StorageRoot, "images", media.StorageName)));
        // The original name is kept for display only, with any path segments stripped.
        Assert.Equal("evil name.png", media.OriginalName);
        Assert.DoesNotContain("..", media.OriginalName);
    }

    [Fact]
    public async Task DisallowedExtension_IsRejected()
    {
        var (service, _) = CreateService();
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("MZ binary")), 0, 9, "upload", "payload.exe");

        var error = await Assert.ThrowsAsync<MediaValidationException>(
            () => service.SaveAsync(file, MediaKind.Image, null, "tester"));
        Assert.Contains("not allowed", error.Message);
    }

    [Fact]
    public async Task ContentTypeMismatch_IsRejected()
    {
        var (service, _) = CreateService();
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("<?php echo 'hi'; ?>")), 0, 20, "upload", "shell.png");

        var error = await Assert.ThrowsAsync<MediaValidationException>(
            () => service.SaveAsync(file, MediaKind.Image, null, "tester"));
        Assert.Contains("does not match", error.Message);
    }

    [Fact]
    public async Task OversizedFile_IsRejected()
    {
        var (service, _) = CreateService(maxImageBytes: 10);

        var error = await Assert.ThrowsAsync<MediaValidationException>(
            () => service.SaveAsync(PngFile(), MediaKind.Image, null, "tester"));
        Assert.Contains("too large", error.Message);
    }

    [Fact]
    public async Task FileInUse_CannotBeDeleted()
    {
        var (service, db) = CreateService();
        var media = await service.SaveAsync(PngFile(), MediaKind.Image, null, "tester");

        var settings = await db.SiteSettings.FirstAsync();
        settings.ResumeMediaId = media.Id;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<MediaInUseException>(() => service.DeleteAsync(media.Id));

        settings.ResumeMediaId = null;
        await db.SaveChangesAsync();
        Assert.True(await service.DeleteAsync(media.Id));
        Assert.False(await db.MediaFiles.AnyAsync(m => m.Id == media.Id));
    }
}
