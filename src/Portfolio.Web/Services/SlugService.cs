using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Portfolio.Web.Services;

public interface ISlugService
{
    string Create(string? desired, string? fallbackSource, string fallbackPrefix);
    Task<string> EnsureUniqueAsync(string slug, Func<string, Task<bool>> isTaken, CancellationToken ct = default);
}

/// <summary>Creates URL-safe slugs and guarantees uniqueness against a caller-supplied check.</summary>
public partial class SlugService : ISlugService
{
    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex InvalidChars();

    [GeneratedRegex("-{2,}", RegexOptions.Compiled)]
    private static partial Regex RepeatedDashes();

    public string Create(string? desired, string? fallbackSource, string fallbackPrefix)
    {
        var slug = Normalize(desired);
        if (slug.Length > 0) return slug;

        slug = Normalize(fallbackSource);
        if (slug.Length > 0) return slug;

        // Bangla (or other non-Latin) titles cannot be transliterated reliably; use a stable prefix.
        return $"{fallbackPrefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6]}";
    }

    public async Task<string> EnsureUniqueAsync(string slug, Func<string, Task<bool>> isTaken, CancellationToken ct = default)
    {
        var candidate = slug;
        var suffix = 2;
        while (await isTaken(candidate))
        {
            candidate = $"{slug}-{suffix++}";
            if (suffix > 500) throw new InvalidOperationException("Could not produce a unique slug.");
        }
        return candidate;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch);
        }

        var cleaned = InvalidChars().Replace(sb.ToString().Normalize(NormalizationForm.FormC), "-");
        cleaned = RepeatedDashes().Replace(cleaned, "-").Trim('-');
        return cleaned.Length > 140 ? cleaned[..140].Trim('-') : cleaned;
    }
}
