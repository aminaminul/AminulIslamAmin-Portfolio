namespace Portfolio.Web.Models.Entities;

public class Experience : PublishableEntity
{
    public string JobTitle { get; set; } = string.Empty;
    public string? JobTitleBn { get; set; }
    public string Company { get; set; } = string.Empty;
    public string? CompanyBn { get; set; }
    public string? CompanyUrl { get; set; }
    public string? EmploymentType { get; set; }
    public string? Location { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Responsibilities { get; set; }
    public string? ResponsibilitiesBn { get; set; }
    public string? Achievements { get; set; }
    public string? AchievementsBn { get; set; }
    public string? Technologies { get; set; }
}

public class Education : PublishableEntity
{
    public string Institution { get; set; } = string.Empty;
    public string? InstitutionBn { get; set; }
    public string? Qualification { get; set; }
    public string? QualificationBn { get; set; }
    public string? Subject { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Result { get; set; }
    public string? Description { get; set; }
    public string? DescriptionBn { get; set; }
    public string? Achievements { get; set; }
}
