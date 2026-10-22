namespace Portfolio.Web.Infrastructure;

public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Adds conservative security headers. Scripts are restricted to same-origin files
    /// (no inline scripts are used anywhere in the application).
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "SAMEORIGIN";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self'; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "font-src 'self' https://fonts.gstatic.com; " +
                "img-src 'self' data:; " +
                "connect-src 'self'; " +
                "form-action 'self'; " +
                "frame-ancestors 'self'; " +
                "base-uri 'self'; " +
                "object-src 'none'";
            await next();
        });
}
