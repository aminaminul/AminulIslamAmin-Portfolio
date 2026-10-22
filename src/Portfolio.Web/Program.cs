using Portfolio.Web;
using Portfolio.Web.Infrastructure;
using System.Globalization;
using System.Threading.RateLimiting;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Portfolio.Web.Data;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.Options;
using Portfolio.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is required. Configure it via environment variables or user secrets.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(3);
        sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
    }));

// ---------- Identity ----------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    // Note: the __Host- prefix requires Secure cookies and HTTPS; the cookie name is kept
    // host-agnostic so HTTP development and IIS deployments behind HTTPS both work.
    options.Cookie.Name = "Portfolio.AdminAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.LoginPath = "/Admin/Account/Login";
    options.LogoutPath = "/Admin/Account/Logout";
    options.AccessDeniedPath = "/Admin/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(15));

// ---------- Options ----------
builder.Services.Configure<MediaOptions>(builder.Configuration.GetSection(MediaOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));

// ---------- Application services ----------
builder.Services.AddMemoryCache();
builder.Services.AddScoped<AppDbContext>();
builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();
builder.Services.AddScoped<ISiteChromeService, SiteChromeService>();
builder.Services.AddScoped<IPortfolioQueryService, PortfolioQueryService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<ISitemapService, SitemapService>();
builder.Services.AddSingleton<ISlugService, SlugService>();
builder.Services.AddSingleton<IMarkdownService, MarkdownService>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

// ---------- Output encoding ----------
// Razor's default encoder escapes every non-ASCII character as a numeric entity, which would send
// all Bangla text as &#x9AE;&#x9C2;… — functionally correct but bloated and hostile to text
// extraction. Allowing the full Unicode range keeps the markup UTF-8 while still encoding HTML.
builder.Services.AddSingleton<HtmlEncoder>(HtmlEncoder.Create(UnicodeRanges.All));

// ---------- Localization ----------
var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("bn")
};

// The resource files live in Resources/ but are compiled with the logical name
// "Portfolio.Web.SharedResource(.bn).resources" (the class sits in namespace Portfolio.Web).
// Setting ResourcesPath would make the localizer look for
// "Portfolio.Web.Resources.SharedResource" instead and every lookup would silently fall back
// to English, so the path must stay empty.
builder.Services.AddLocalization();
builder.Services.AddControllersWithViews(options =>
    {
        // Every state-changing request must carry a valid antiforgery token.
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource));
    });

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en", "en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders =
    [
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider { CookieName = ".AspNetCore.Culture" }
    ];
});

// ---------- Data protection ----------
var keysPath = builder.Configuration["DataProtection:KeysPath"] ?? "App_Data/keys";
var keysFullPath = Path.IsPathRooted(keysPath)
    ? keysPath
    : Path.Combine(builder.Environment.ContentRootPath, keysPath);
Directory.CreateDirectory(keysFullPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFullPath))
    .SetApplicationName("PortfolioCMS");

// ---------- Rate limiting ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("contact", context =>
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                               | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    // Restrict trusted proxies to loopback interfaces by default so external clients cannot spoof X-Forwarded-For
    options.KnownProxies.Add(System.Net.IPAddress.Loopback);
    options.KnownProxies.Add(System.Net.IPAddress.IPv6Loopback);
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "Portfolio.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// ---------- Health checks ----------
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

// ---------- Startup database work ----------
await DbInitializer.InitializeAsync(app.Services, app.Configuration, app.Logger);

// ---------- Pipeline ----------
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseSecurityHeaders();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var isDev = app.Environment.IsDevelopment();
        ctx.Context.Response.Headers.CacheControl = isDev
            ? "no-cache, must-revalidate"
            : "public,max-age=604800";
    }
});

app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");

// The area route is registered with a negative order so that link generation inside the admin
// area resolves to /Admin/... endpoints. Public controllers declare absolute attribute routes
// (for example [HttpGet("/projects")]); without this, RedirectToAction from an admin controller
// whose public namesake shares a controller/action name would generate the public URL instead.
app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .Add(builder =>
    {
        if (builder is RouteEndpointBuilder endpoint)
        {
            endpoint.Order = -100;
        }
    });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program;
