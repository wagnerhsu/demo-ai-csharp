using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Text;

namespace MarkdownViewer.Services;

public record HeadingItem(int Level, string Text, string Anchor);

public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
            .Build();
    }

    public string RenderToHtml(string markdown)
    {
        var html = Markdown.ToHtml(markdown, _pipeline);
        // inject highlight.js language classes for code blocks
        return html;
    }

    public List<HeadingItem> ExtractOutline(string markdown)
    {
        var document = Markdown.Parse(markdown, _pipeline);
        var headings = new List<HeadingItem>();

        foreach (var block in document)
        {
            if (block is HeadingBlock heading && heading.Level <= 4)
            {
                var text = ExtractText(heading.Inline);
                var anchor = ToAnchor(text);
                headings.Add(new HeadingItem(heading.Level, text, anchor));
            }
        }

        return headings;
    }

    private static string ExtractText(ContainerInline? inline)
    {
        if (inline is null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var item in inline)
        {
            if (item is LiteralInline literal)
                sb.Append(literal.Content);
            else if (item is ContainerInline container)
                sb.Append(ExtractText(container));
        }
        return sb.ToString();
    }

    // mirrors GitHub's anchor generation
    private static string ToAnchor(string text)
    {
        var sb = new StringBuilder();
        foreach (var c in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (c == ' ' || c == '-')
                sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }
}
