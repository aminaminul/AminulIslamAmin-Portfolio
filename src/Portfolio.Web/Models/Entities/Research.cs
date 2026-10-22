namespace Portfolio.Web.Models.Entities;

public class Research : PublishableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? TitleBn { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? Abstract { get; set; }
    public string? AbstractBn { get; set; }
    public string? Introduction { get; set; }
    public string? Methodology { get; set; }
    public string? Tools { get; set; }
    public string? Datasets { get; set; }
    public string? Experiments { get; set; }
    public string? Findings { get; set; }
    public string? Discussion { get; set; }
    public string? Limitations { get; set; }
    public string? Conclusion { get; set; }
    public string? FutureWork { get; set; }
    public string? References { get; set; }
    public string? Keywords { get; set; }

    public int? CategoryId { get; set; }
    public ResearchCategory? Category { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? PublicationDate { get; set; }
    public string? PublicationVenue { get; set; }
    public string? Journal { get; set; }
    public string? Doi { get; set; }
    public string? ExternalUrl { get; set; }

    public int? CoverImageMediaId { get; set; }
    public MediaFile? CoverImage { get; set; }
    public int? PdfMediaId { get; set; }
    public MediaFile? Pdf { get; set; }

    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }

    public ICollection<ResearchAuthor> Authors { get; set; } = new List<ResearchAuthor>();
    public ICollection<ResearchTag> Tags { get; set; } = new List<ResearchTag>();
    public ICollection<ResearchRelated> Related { get; set; } = new List<ResearchRelated>();
}

public class ResearchCategory : BaseEntity, IVisibleContent
{
    public string Name { get; set; } = string.Empty;
    public string? NameBn { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public ICollection<Research> Records { get; set; } = new List<Research>();
}

public class ResearchAuthor : BaseEntity
{
    public int ResearchId { get; set; }
    public Research? Research { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Affiliation { get; set; }
    public string? Role { get; set; }
    public bool IsOwner { get; set; }
    public int DisplayOrder { get; set; }
}

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ICollection<ResearchTag> Research { get; set; } = new List<ResearchTag>();
}

public class ResearchTag
{
    public int ResearchId { get; set; }
    public Research? Research { get; set; }
    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}

public class ResearchRelated
{
    public int ResearchId { get; set; }
    public Research? Research { get; set; }
    public int RelatedResearchId { get; set; }
    public Research? RelatedResearch { get; set; }
}
