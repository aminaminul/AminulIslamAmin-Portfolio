using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Web.Models.Entities;

namespace Portfolio.Tests.Infrastructure;

/// <summary>
/// Data inserts used by the tests. Every record is namespaced with a unique token so tests that
/// share one database stay independent of each other's rows.
/// </summary>
public static class TestData
{
    public static string Token() => Guid.NewGuid().ToString("N")[..8];

    public static async Task<Project> AddProjectAsync(PortfolioAppFactory factory, ContentStatus status, string? token = null)
    {
        token ??= Token();
        var project = new Project
        {
            Title = $"Test project {token}",
            Slug = $"test-project-{token}",
            Summary = $"Summary for {token}",
            Description = $"## Description\n\nBody for {token}",
            Category = "Testing",
            Status = status,
            PublishedAt = status == ContentStatus.Published ? DateTime.UtcNow : null,
            DisplayOrder = 1
        };

        await factory.WithDbAsync(async db =>
        {
            db.Projects.Add(project);
            await db.SaveChangesAsync();
        });

        return project;
    }

    public static async Task<Research> AddResearchAsync(PortfolioAppFactory factory, ContentStatus status, string? token = null)
    {
        token ??= Token();
        var research = new Research
        {
            Title = $"Test research {token}",
            Slug = $"test-research-{token}",
            Abstract = $"Abstract for {token}",
            Keywords = $"{token}, testing",
            Findings = "Findings body",
            Status = status,
            PublishedAt = status == ContentStatus.Published ? DateTime.UtcNow : null,
            PublicationDate = status == ContentStatus.Published ? new DateTime(2025, 3, 1) : null,
            DisplayOrder = 1
        };

        await factory.WithDbAsync(async db =>
        {
            db.Research.Add(research);
            db.ResearchAuthors.Add(new ResearchAuthor
            {
                ResearchId = research.Id,
                Name = "Test Author",
                Research = research,
                DisplayOrder = 1
            });
            await db.SaveChangesAsync();
        });

        return research;
    }

    public static async Task<int> CountContactMessagesAsync(PortfolioAppFactory factory, string marker)
    {
        using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Portfolio.Web.Data.AppDbContext>();
        return await db.ContactMessages.CountAsync(m => m.Subject == marker || m.Message.Contains(marker));
    }

    public static async Task<Profile> GetProfileAsync(PortfolioAppFactory factory)
    {
        using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Portfolio.Web.Data.AppDbContext>();
        return await db.Profiles.AsNoTracking().OrderBy(p => p.Id).FirstAsync();
    }
}
