using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Web.Data;
using Portfolio.Web.Models.Entities;

namespace Portfolio.Tests.Infrastructure;

/// <summary>
/// Boots the real application (Program.cs pipeline, Identity, Razor views, services) against a
/// throw-away SQLite database so the tests exercise public pages and the admin panel end to end.
/// SQL Server specific behaviour (migrations) is documented and verified separately.
/// </summary>
public sealed partial class PortfolioAppFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@portfolio.test";
    public const string AdminPassword = "Str0ng!Passw0rd#2026";
    public const string StandardUserEmail = "visitor@portfolio.test";
    public const string StandardUserPassword = "An0ther!Passw0rd#2026";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public string MediaRoot { get; } =
        Path.Combine(Path.GetTempPath(), "portfolio-cms-tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused;Database=unused;");
        builder.UseSetting("AdminSeed:Email", AdminEmail);
        builder.UseSetting("AdminSeed:Password", AdminPassword);
        builder.UseSetting("AdminSeed:DisplayName", "Test Administrator");
        builder.UseSetting("Media:RootPath", MediaRoot);
        builder.UseSetting("DataProtection:KeysPath", Path.Combine(MediaRoot, "keys"));
        builder.UseSetting("Email:Host", "");
        builder.UseSetting("Site:PublicBaseUrl", "https://portfolio.test");

        builder.ConfigureServices(services =>
        {
            // Replace the SQL Server context registered by Program.cs with SQLite.
            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(AppDbContext)
                                     || d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                                     || d.ServiceType == typeof(DbContextOptions)
                                     || (d.ServiceType.IsGenericType
                                         && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)
                                         && d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext)))
                         .ToList())
            {
                services.Remove(descriptor);
            }

            _connection.Open();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    /// <summary>HTTP client that does not follow redirects so authorization behaviour can be asserted.</summary>
    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
        BaseAddress = new Uri("https://portfolio.test")
    });

    public HttpClient CreateBrowserClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = true,
        HandleCookies = true,
        BaseAddress = new Uri("https://portfolio.test")
    });

    public IServiceScope CreateScope() => Services.CreateScope();

    /// <summary>Runs an action against a fresh scoped DbContext.</summary>
    public async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        using var scope = CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public T GetService<T>() where T : notnull
    {
        using var scope = CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>Submits the admin login form and returns the post-login client.</summary>
    public async Task<HttpClient> SignInAdminAsync()
    {
        var client = CreateApiClient();
        var token = await GetAntiforgeryTokenAsync(client, "/Admin/Account/Login");
        var response = await client.PostAsync("/Admin/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = AdminEmail,
            ["Password"] = AdminPassword,
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = token
        }));

        if (response.StatusCode is not (HttpStatusCode.Redirect or HttpStatusCode.Found))
            throw new InvalidOperationException($"Admin sign-in failed with {(int)response.StatusCode}.");

        return client;
    }

    /// <summary>Creates a signed-in account that does not hold the Administrator role.</summary>
    public async Task<HttpClient> SignInStandardUserAsync()
    {
        var client = CreateApiClient();
        var token = await GetAntiforgeryTokenAsync(client, "/Admin/Account/Login");
        var response = await client.PostAsync("/Admin/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = StandardUserEmail,
            ["Password"] = StandardUserPassword,
            ["__RequestVerificationToken"] = token
        }));

        if (response.StatusCode is not (HttpStatusCode.Redirect or HttpStatusCode.Found))
            throw new InvalidOperationException($"Standard user sign-in failed with {(int)response.StatusCode}.");

        return client;
    }

    /// <summary>Creates (once) an authenticated account without the Administrator role.</summary>
    public async Task EnsureStandardUserAsync()
    {
        using var scope = CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await users.FindByEmailAsync(StandardUserEmail) is not null) return;

        var user = new ApplicationUser
        {
            UserName = StandardUserEmail,
            Email = StandardUserEmail,
            EmailConfirmed = true,
            DisplayName = "Standard Visitor",
            CreatedAt = DateTime.UtcNow
        };
        var result = await users.CreateAsync(user, StandardUserPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException("Could not create the standard test user: " +
                                                string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    /// <summary>Reads the antiforgery token rendered on a page (all admin forms carry one).</summary>
    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = TokenRegex().Match(html);
        if (!match.Success)
            throw new InvalidOperationException($"No antiforgery token found on {path}.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    public async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string path, string tokenPath,
        IDictionary<string, string?> fields)
    {
        var token = await GetAntiforgeryTokenAsync(client, tokenPath);
        var payload = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in fields)
        {
            if (pair.Value is not null) payload[pair.Key] = pair.Value;
        }
        payload["__RequestVerificationToken"] = token;
        return await client.PostAsync(path, new FormUrlEncodedContent(payload));
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _connection.Dispose();
        try
        {
            if (Directory.Exists(MediaRoot)) Directory.Delete(MediaRoot, recursive: true);
        }
        catch (IOException)
        {
            // Temporary test artifacts are cleaned up on a best-effort basis.
        }
    }
}

[CollectionDefinition(Name)]
public class AppCollection : ICollectionFixture<PortfolioAppFactory>
{
    public const string Name = "portfolio-app";
}
