using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Portfolio.Web.Models.Entities;
using Portfolio.Web.Models.Options;
using Portfolio.Web.Services;

namespace Portfolio.Web.Data;

/// <summary>
/// Idempotent startup work: schema creation (development/testing only), structural defaults and
/// secure administrator provisioning from configuration.
/// </summary>
public static class DbInitializer
{
    public const string AdministratorRole = "Administrator";

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        var provider = db.Database.ProviderName ?? string.Empty;
        var isSqlServer = provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);

        if (isSqlServer)
        {
            if (databaseOptions.MigrateOnStartup)
            {
                logger.LogInformation("Applying pending EF Core migrations...");
                await db.Database.MigrateAsync();
            }
            else if (!await db.Database.CanConnectAsync())
            {
                logger.LogError("The SQL Server database is not reachable. Check the ConnectionStrings:DefaultConnection value.");
                return;
            }
        }
        else
        {
            // Non-SQL Server providers are used only by automated tests.
            await db.Database.EnsureCreatedAsync();
        }

        await SeedStructureAsync(db, logger);
        await ProvisionAdministratorAsync(sp, configuration, logger);
    }

    private static async Task SeedStructureAsync(AppDbContext db, ILogger logger)
    {
        if (!await db.SiteSettings.AnyAsync())
        {
            db.SiteSettings.Add(SiteSettingsService.Defaults());
            logger.LogInformation("Seeded default site settings.");
        }

        if (!await db.Profiles.AnyAsync())
        {
            db.Profiles.Add(new Profile { FullName = string.Empty });
            logger.LogInformation("Created an empty profile row; fill it in from the admin panel.");
        }

        if (!await db.HeroSettings.AnyAsync())
        {
            db.HeroSettings.Add(new HeroSettings());
        }

        if (!await db.NavigationItems.AnyAsync())
        {
            db.NavigationItems.AddRange(SiteChromeService.DefaultNavigation());
            logger.LogInformation("Seeded default navigation items.");
        }

        await db.SaveChangesAsync();
    }

    private static async Task ProvisionAdministratorAsync(IServiceProvider sp, IConfiguration configuration, ILogger logger)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var options = configuration.GetSection(AdminSeedOptions.SectionName).Get<AdminSeedOptions>() ?? new AdminSeedOptions();

        if (!await roleManager.RoleExistsAsync(AdministratorRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(AdministratorRole));
            if (!roleResult.Succeeded)
                logger.LogError("Could not create the {Role} role: {Errors}", AdministratorRole,
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning("No administrator seed credentials supplied (AdminSeed:Email / AdminSeed:Password). " +
                              "If no administrator exists yet, configure them and restart to provision one.");
            return;
        }

        var existing = await userManager.FindByEmailAsync(options.Email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, AdministratorRole))
                await userManager.AddToRoleAsync(existing, AdministratorRole);
            logger.LogInformation("Administrator {Email} already exists.", options.Email);
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = options.Email,
            Email = options.Email,
            EmailConfirmed = true,
            DisplayName = string.IsNullOrWhiteSpace(options.DisplayName) ? "Administrator" : options.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, options.Password);
        if (!result.Succeeded)
        {
            logger.LogError("Administrator provisioning failed: {Errors}. Use a longer password that meets the configured policy.",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, AdministratorRole);
        logger.LogInformation("Provisioned administrator {Email}.", options.Email);
    }
}
