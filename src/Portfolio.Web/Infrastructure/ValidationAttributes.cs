using System.ComponentModel.DataAnnotations;

namespace Portfolio.Web.Infrastructure;

/// <summary>
/// Accepts either a local absolute path ("/projects") or an absolute http(s) URL.
/// Rejects javascript:, data: and protocol-relative URLs.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class UrlOrPathAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success;

        var text = value.ToString()!.Trim();

        if (text.StartsWith('/') && !text.StartsWith("//") && !text.StartsWith("/\\"))
            return ValidationResult.Success;

        if (Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            return ValidationResult.Success;

        return new ValidationResult("Enter a site path starting with / or a full http(s) URL.");
    }
}
