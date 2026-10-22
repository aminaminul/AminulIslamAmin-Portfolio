using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using Markdig.Syntax;

namespace Portfolio.Web.Services;

public interface IMarkdownService
{
    string ToHtml(string? markdown);
}

/// <summary>
/// Renders administrator-authored Markdown with raw HTML disabled, so no submitted HTML can execute.
/// Links get rel="nofollow noopener" and external links open safely.
/// </summary>
public class MarkdownService : IMarkdownService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .UseAutoLinks()
        .UseEmphasisExtras()
        .UseTaskLists()
        .Build();

    public string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;

        var document = Markdown.Parse(markdown, Pipeline);
        foreach (var link in document.Descendants<LinkInline>())
        {
            var attributes = link.TryGetAttributes() ?? new HtmlAttributes();
            attributes.AddProperty("rel", "nofollow noopener");
            if (link.Url is not null && link.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                attributes.AddProperty("target", "_blank");
            link.SetAttributes(attributes);
        }

        return Markdown.ToHtml(document, Pipeline);
    }
}
