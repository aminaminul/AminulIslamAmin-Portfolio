using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Web.Models.ViewModels;

namespace Portfolio.Web.Controllers;

[AllowAnonymous]
public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger) => _logger = logger;

    [Route("error/{statusCode:int}")]
    public IActionResult Index(int statusCode)
    {
        var original = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        _logger.LogInformation("Responded {StatusCode} for {Path}", statusCode,
            original?.OriginalPath ?? Request.Path.Value);

        var (title, message) = statusCode switch
        {
            400 => ("Bad request", "The request could not be processed."),
            401 => ("Sign in required", "You need to sign in to view that page."),
            403 => ("Access denied", "You do not have permission to view that page."),
            404 => ("Page not found", "The page you are looking for does not exist, or the content is no longer published."),
            429 => ("Too many requests", "Please wait a moment before trying again."),
            _ => ("Something went wrong", "An unexpected error occurred. Please try again later.")
        };

        if (statusCode >= 500)
            _logger.LogError("Unhandled server error. TraceId: {TraceId}", Activity.Current?.Id ?? HttpContext.TraceIdentifier);

        ViewData["Seo"] = new SeoModel($"{statusCode} - {title}", message, NoIndex: true);
        Response.StatusCode = statusCode;
        return View("Error", new ErrorViewModel { StatusCode = statusCode, Title = title, Message = message });
    }
}
