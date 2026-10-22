namespace Portfolio.Web.Models.Entities;

public class SkillCategory : BaseEntity, IVisibleContent
{
    public string Name { get; set; } = string.Empty;
    public string? NameBn { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
}

public class Skill : BaseEntity, IVisibleContent
{
    public string Name { get; set; } = string.Empty;
    public string? NameBn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionBn { get; set; }
    public int? CategoryId { get; set; }
    public SkillCategory? Category { get; set; }
    public string? IconName { get; set; }
    /// <summary>Optional 0-100 proficiency supplied by the owner; null hides the indicator.</summary>
    public int? Proficiency { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
}
