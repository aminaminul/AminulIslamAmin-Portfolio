namespace Portfolio.Web.Models.Entities;

/// <summary>Publication state shared by every content type that supports drafts.</summary>
public enum ContentStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

/// <summary>Lifecycle of a project, as supplied by the administrator. Never inferred.</summary>
public enum ProjectStage
{
    Planned = 0,
    InProgress = 1,
    Completed = 2,
    Maintained = 3,
    Archived = 4
}

public enum MediaKind
{
    Image = 0,
    Document = 1
}

public enum EmailDeliveryState
{
    NotAttempted = 0,
    NotConfigured = 1,
    Sent = 2,
    Failed = 3
}

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Base class for records that are drafted, published and possibly archived.</summary>
public abstract class PublishableEntity : BaseEntity
{
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; }

    public bool IsPublished => Status == ContentStatus.Published;
}

/// <summary>Marks entities whose public visibility is controlled by a boolean flag.</summary>
public interface IVisibleContent
{
    bool IsVisible { get; set; }
    int DisplayOrder { get; set; }
}
