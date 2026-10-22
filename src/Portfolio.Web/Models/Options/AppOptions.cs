namespace Portfolio.Web.Models.Options;

/// <summary>Storage and validation limits for uploaded media.</summary>
public class MediaOptions
{
    public const string SectionName = "Media";

    /// <summary>Root folder for uploads. Relative paths resolve against the content root.</summary>
    public string RootPath { get; set; } = "App_Data/media";
    public long MaxImageBytes { get; set; } = 8 * 1024 * 1024;
    public long MaxDocumentBytes { get; set; } = 20 * 1024 * 1024;
    public string[] ImageExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    public string[] DocumentExtensions { get; set; } = [".pdf"];
    public string PublicRequestPath { get; set; } = "/media";
}

/// <summary>Optional SMTP settings. Email is disabled unless a host and sender are provided.</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
    public string? ToAddress { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}

/// <summary>Initial administrator provisioning. Never supplied with a default password in source.</summary>
public class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? DisplayName { get; set; }
}

/// <summary>Controls optional startup database work.</summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Apply pending EF Core migrations at startup (development convenience; explicit in production).</summary>
    public bool MigrateOnStartup { get; set; }
    public bool EnsureCreatedInTesting { get; set; } = true;
}
