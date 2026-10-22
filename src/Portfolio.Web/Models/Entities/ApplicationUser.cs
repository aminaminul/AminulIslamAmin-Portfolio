using Microsoft.AspNetCore.Identity;

namespace Portfolio.Web.Models.Entities;

/// <summary>Administrator account. Registration is disabled; accounts are provisioned by a seeding step.</summary>
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
}
