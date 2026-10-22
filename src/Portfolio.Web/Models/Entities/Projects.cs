namespace Portfolio.Web.Models.Entities;

public class Project : PublishableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? TitleBn { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? SummaryBn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionBn { get; set; }
    public string? ProblemStatement { get; set; }
    public string? Solution { get; set; }
    public string? Features { get; set; }
    public string? Category { get; set; }

    public int? CoverImageMediaId { get; set; }
    public MediaFile? CoverImage { get; set; }

    public string? RepositoryUrl { get; set; }
    public string? LiveUrl { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStage Stage { get; set; } = ProjectStage.Completed;

    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }

    public ICollection<ProjectImage> Images { get; set; } = new List<ProjectImage>();
    public ICollection<ProjectTechnology> Technologies { get; set; } = new List<ProjectTechnology>();
}

public class ProjectImage : BaseEntity
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public int MediaFileId { get; set; }
    public MediaFile? MediaFile { get; set; }
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; }
}

public class Technology : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ICollection<ProjectTechnology> Projects { get; set; } = new List<ProjectTechnology>();
}

public class ProjectTechnology
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public int TechnologyId { get; set; }
    public Technology? Technology { get; set; }
}
