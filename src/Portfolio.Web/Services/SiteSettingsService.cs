using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Portfolio.Web.Data;
using Portfolio.Web.Models.Entities;

namespace Portfolio.Web.Services;

public interface ISiteSettingsService
{
    Task<SiteSettings> GetAsync(CancellationToken ct = default);
    Task<SiteSettings> GetTrackedAsync(CancellationToken ct = default);
    void InvalidateCache();
}

/// <summary>
/// Provides the single site-settings row. Reads are cached and the cache is explicitly
/// invalidated whenever the administrator saves settings, so published changes appear immediately.
/// </summary>
public class SiteSettingsService : ISiteSettingsService
{
    private const string CacheKey = "site-settings:v1";
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SiteSettingsService> _logger;
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public SiteSettingsService(AppDbContext db, IMemoryCache cache, ILogger<SiteSettingsService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<SiteSettings> GetAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out SiteSettings? cached) && cached is not null)
            return cached;

        await Gate.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached is not null)
                return cached;

            var settings = await _db.SiteSettings.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
            if (settings is null)
            {
                settings = Defaults();
                _db.SiteSettings.Add(settings);
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Created default site settings row {SettingsId}", settings.Id);
            }

            _cache.Set(CacheKey, settings, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
            });
            return settings;
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<SiteSettings> GetTrackedAsync(CancellationToken ct = default)
    {
        var settings = await _db.SiteSettings.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = Defaults();
            _db.SiteSettings.Add(settings);
            await _db.SaveChangesAsync(ct);
        }
        return settings;
    }

    public void InvalidateCache() => _cache.Remove(CacheKey);

    public static SiteSettings Defaults() => new()
    {
        SiteName = "Portfolio",
        Tagline = "A personal portfolio managed from the admin panel",
        FooterDescription = null,
        CopyrightText = null,
        IsAvailable = false,
        ShowContactForm = true,
        ShowResumeSection = true,
        ResumeButtonLabel = "Download CV",
        DefaultTheme = "system",
        SeoTitle = null,
        SeoDescription = null
    };
}
