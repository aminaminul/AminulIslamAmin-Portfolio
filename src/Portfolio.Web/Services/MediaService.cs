using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Portfolio.Web.Data;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.Options;

namespace Portfolio.Web.Services;

public class MediaValidationException(string message) : Exception(message);
public class MediaInUseException(string message, IReadOnlyList<MediaUsage> usages) : Exception(message)
{
    public IReadOnlyList<MediaUsage> Usages { get; } = usages;
}

public record MediaUsage(string Description, string AdminPath);

public record ResolvedMedia(string FullPath, string ContentType, string DownloadName, MediaKind Kind);

public interface IMediaService
{
    Task<MediaFile> SaveAsync(IFormFile file, MediaKind kind, string? altText, string? uploadedBy, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string? rootPathOverride = null, CancellationToken ct = default);
    Task<IReadOnlyList<MediaUsage>> GetUsagesAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<MediaFile>> ListAsync(MediaKind kind, CancellationToken ct = default);
    Task<MediaFile?> GetAsync(int id, CancellationToken ct = default);
    ResolvedMedia? Resolve(MediaFile media);
    string StorageRoot { get; }
}

public class MediaService : IMediaService
{
    private readonly AppDbContext _db;
    private readonly MediaOptions _options;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MediaService> _logger;

    public MediaService(AppDbContext db, IOptions<MediaOptions> options, IWebHostEnvironment env, ILogger<MediaService> logger)
    {
        _db = db;
        _options = options.Value;
        _env = env;
        _logger = logger;
    }

    public string StorageRoot =>
        Path.GetFullPath(Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(_env.ContentRootPath, _options.RootPath));

    public async Task<MediaFile> SaveAsync(IFormFile file, MediaKind kind, string? altText, string? uploadedBy, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            throw new MediaValidationException("Choose a file to upload.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = kind == MediaKind.Image ? _options.ImageExtensions : _options.DocumentExtensions;
        var maxBytes = kind == MediaKind.Image ? _options.MaxImageBytes : _options.MaxDocumentBytes;

        if (!allowed.Contains(extension))
            throw new MediaValidationException(
                $"Files of type '{extension}' are not allowed. Allowed: {string.Join(", ", allowed)}.");

        if (file.Length > maxBytes)
            throw new MediaValidationException(
                $"The file is too large. Maximum allowed size is {maxBytes / (1024 * 1024)} MB.");

        await using (var probe = file.OpenReadStream())
        {
            if (!await SignatureMatchesAsync(probe, extension, ct))
                throw new MediaValidationException("The file content does not match its extension.");
        }

        var folder = kind == MediaKind.Image ? "images" : "documents";
        var storageName = $"{Guid.NewGuid():N}{extension}";
        var folderPath = Path.Combine(StorageRoot, folder);
        Directory.CreateDirectory(folderPath);
        var fullPath = Path.Combine(folderPath, storageName);

        // Write to a temporary file first so an interrupted upload never leaves a half-written asset.
        var tempPath = fullPath + ".tmp";
        try
        {
            await using (var target = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            await using (var source = file.OpenReadStream())
            {
                await source.CopyToAsync(target, ct);
            }
            File.Move(tempPath, fullPath);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }

        var media = new MediaFile
        {
            StorageName = storageName,
            OriginalName = Path.GetFileName(file.FileName)[..Math.Min(Path.GetFileName(file.FileName).Length, 255)],
            ContentType = ContentTypeFor(extension),
            Extension = extension,
            SizeBytes = file.Length,
            Kind = kind,
            AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim(),
            UploadedBy = uploadedBy
        };

        try
        {
            _db.MediaFiles.Add(media);
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            TryDelete(fullPath);
            throw;
        }

        _logger.LogInformation("Stored media {MediaId} ({StorageName}, {Bytes} bytes) for {User}", media.Id, storageName, media.SizeBytes, uploadedBy);
        return media;
    }

    public async Task<IReadOnlyList<MediaUsage>> GetUsagesAsync(int id, CancellationToken ct = default)
    {
        var usages = new List<MediaUsage>();

        if (await _db.SiteSettings.AnyAsync(s => s.LogoMediaId == id, ct)) usages.Add(new("Site logo", "/Admin/Settings"));
        if (await _db.SiteSettings.AnyAsync(s => s.OgImageMediaId == id, ct)) usages.Add(new("SEO social image", "/Admin/Settings/Seo"));
        if (await _db.SiteSettings.AnyAsync(s => s.ResumeMediaId == id, ct)) usages.Add(new("Resume / CV", "/Admin/Resume"));
        if (await _db.Profiles.AnyAsync(s => s.PhotoMediaId == id, ct)) usages.Add(new("Profile photo", "/Admin/Profile"));
        if (await _db.HeroSettings.AnyAsync(s => s.ImageMediaId == id, ct)) usages.Add(new("Hero image", "/Admin/Hero"));
        if (await _db.AboutSections.AnyAsync(s => s.ImageMediaId == id, ct)) usages.Add(new("About section image", "/Admin/About"));
        if (await _db.Projects.AnyAsync(s => s.CoverImageMediaId == id, ct)) usages.Add(new("Project cover image", "/Admin/Projects"));
        if (await _db.ProjectImages.AnyAsync(s => s.MediaFileId == id, ct)) usages.Add(new("Project gallery image", "/Admin/Projects"));
        if (await _db.Research.AnyAsync(s => s.CoverImageMediaId == id, ct)) usages.Add(new("Research cover image", "/Admin/Research"));
        if (await _db.Research.AnyAsync(s => s.PdfMediaId == id, ct)) usages.Add(new("Research document", "/Admin/Research"));
        if (await _db.Certifications.AnyAsync(s => s.ImageMediaId == id, ct)) usages.Add(new("Certification image", "/Admin/Certifications"));
        if (await _db.Achievements.AnyAsync(s => s.ImageMediaId == id, ct)) usages.Add(new("Achievement image", "/Admin/Achievements"));
        if (await _db.Testimonials.AnyAsync(s => s.AvatarMediaId == id, ct)) usages.Add(new("Testimonial avatar", "/Admin/Testimonials"));

        return usages;
    }

    public async Task<bool> DeleteAsync(int id, string? rootPathOverride = null, CancellationToken ct = default)
    {
        var media = await _db.MediaFiles.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (media is null) return false;

        var usages = await GetUsagesAsync(id, ct);
        if (usages.Count > 0)
            throw new MediaInUseException("This file is still used by other content. Remove those references first.", usages);

        var fullPath = BuildPath(rootPathOverride ?? StorageRoot, media);
        _db.MediaFiles.Remove(media);
        await _db.SaveChangesAsync(ct);
        TryDelete(fullPath);
        return true;
    }

    public async Task<IReadOnlyList<MediaFile>> ListAsync(MediaKind kind, CancellationToken ct = default) =>
        await _db.MediaFiles.AsNoTracking()
            .Where(m => m.Kind == kind)
            .OrderByDescending(m => m.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

    public async Task<MediaFile?> GetAsync(int id, CancellationToken ct = default) =>
        await _db.MediaFiles.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);

    public ResolvedMedia? Resolve(MediaFile media)
    {
        var path = BuildPath(StorageRoot, media);
        if (!File.Exists(path)) return null;

        return new ResolvedMedia(
            path,
            string.IsNullOrWhiteSpace(media.ContentType) ? ContentTypeFor(media.Extension) : media.ContentType,
            media.OriginalName,
            media.Kind);
    }

    private static string BuildPath(string root, MediaFile media)
    {
        var folder = media.Kind == MediaKind.Image ? "images" : "documents";
        var candidate = Path.GetFullPath(Path.Combine(root, folder, Path.GetFileName(media.StorageName)));
        var rootFull = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            throw new MediaValidationException("Invalid media path.");
        return candidate;
    }

    private static async Task<bool> SignatureMatchesAsync(Stream stream, string extension, CancellationToken ct)
    {
        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (read < 4) return false;

        return extension switch
        {
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            ".gif" => header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46,
            ".webp" => header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                       && read >= 12 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
            ".pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46,
            _ => false
        };
    }

    private static string ContentTypeFor(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete media file at {Path}", path);
        }
    }
}
