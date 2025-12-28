using Markdig;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace FxNyaa;

public static class DiscordFlavouredMarkdown
{
    public static string ParseMarkdown(string content, out List<(string Url, string AltText)> images)
    {
        var markdown = System.Net.WebUtility.HtmlDecode(content);

        var pipeline = new MarkdownPipelineBuilder()
            .UseAutoLinks()
            // .UsePipeTables()
            .Build();

        var writer = new StringWriter();

        var renderer = new NormalizeRenderer(writer);

        renderer.ObjectRenderers.Clear();

        // default block renderers
        renderer.ObjectRenderers.Add(new Markdig.Renderers.Normalize.CodeBlockRenderer());
        renderer.ObjectRenderers.Add(new DiscordFlavouredListRenderer());
        renderer.ObjectRenderers.Add(new Markdig.Renderers.Normalize.HeadingRenderer());
        renderer.ObjectRenderers.Add(new Markdig.Renderers.Normalize.HtmlBlockRenderer());
        renderer.ObjectRenderers.Add(new Markdig.Renderers.Normalize.ParagraphRenderer());
        renderer.ObjectRenderers.Add(new DiscordFlavouredQuoteBlockRenderer());
        renderer.ObjectRenderers.Add(new DiscordFlavouredThematicBreakRenderer());
        // renderer.ObjectRenderers.Add(new DiscordFlavouredTableRenderer());

        // default inline renderers
        renderer.ObjectRenderers.Add(new Markdig.Renderers.Normalize.Inlines.AutolinkInlineRenderer());
        renderer.ObjectRenderers.Add(new Markdig.Renderers.Normalize.Inlines.CodeInlineRenderer());
        renderer.ObjectRenderers.Add(new Markdig.Renderers.Normalize.Inlines.DelimiterInlineRenderer());
        renderer.ObjectRenderers.Add(new DiscordFlavouredEmphasisRenderer());
        renderer.ObjectRenderers.Add(new Markdig.Renderers.Normalize.Inlines.LineBreakInlineRenderer());
        renderer.ObjectRenderers.Add(new Markdig.Renderers.Normalize.Inlines.NormalizeHtmlInlineRenderer());
        renderer.ObjectRenderers.Add(new Markdig.Renderers.Normalize.Inlines.NormalizeHtmlEntityInlineRenderer());
        renderer.ObjectRenderers.Add(new DiscordFlavouredLinkRenderer());
        renderer.ObjectRenderers.Add(new Markdig.Renderers.Normalize.Inlines.LiteralInlineRenderer());

        var document = Markdown.Parse(markdown, pipeline);

        images = [];

        foreach (var descendant in document.Descendants())
        {
            if (descendant is LinkInline { IsImage: true, Url: not null } linkInline)
            {
                var altText = "";

                foreach (var child in linkInline)
                {
                    if (child is LiteralInline literal)
                    {
                        altText += literal.Content.ToString();
                    }
                }

                images.Add(new ValueTuple<string, string>(linkInline.Url, altText));
            }
        }

        renderer.Render(document);
        writer.Flush();

        var str = writer.ToString();

        return str;
    }
}

// wouldn't call these nearly as good as the built-in ones, but it should be good enough for discord
public class DiscordFlavouredEmphasisRenderer : NormalizeObjectRenderer<EmphasisInline>
{
    protected override void Write(NormalizeRenderer renderer, EmphasisInline obj)
    {
        var tag = obj.DelimiterCount == 2 ? "strong" : "em";
        renderer.Write($"<{tag}>");
        renderer.WriteChildren(obj);
        renderer.Write($"</{tag}>");
    }
}

public class DiscordFlavouredLinkRenderer : NormalizeObjectRenderer<LinkInline>
{
    protected override void Write(NormalizeRenderer renderer, LinkInline link)
    {
        if (link.IsImage) return;

        var url = link.Url?.Replace("\"", "&quot;");

        renderer.Write($"<a href=\"{url}\">");
        renderer.WriteChildren(link);
        renderer.Write($"</a>");
    }
}

public class DiscordFlavouredListRenderer : NormalizeObjectRenderer<ListBlock>
{
    protected override void Write(NormalizeRenderer renderer, ListBlock listBlock)
    {
        renderer.EnsureLine();

        if (listBlock.IsOrdered)
        {
            renderer.Write("<ol");
            if (listBlock.BulletType != '1')
            {
                renderer.Write(" type=\"");
                renderer.Write(listBlock.BulletType);
                renderer.Write('"');
            }

            if (listBlock.OrderedStart is not null && listBlock.OrderedStart != "1")
            {
                renderer.Write(" start=\"");
                renderer.Write(listBlock.OrderedStart);
                renderer.Write('"');
            }

            renderer.Write('>');
        }
        else
        {
            renderer.Write("<ul>");
        }


        foreach (var item in listBlock)
        {
            var listItem = (ListItemBlock)item;

            renderer.Write("<li>");

            renderer.WriteChildren(listItem);

            renderer.Write("</li>");
        }


        renderer.WriteLine(listBlock.IsOrdered ? "</ol>" : "</ul>");

        renderer.EnsureLine();
    }
}

public class DiscordFlavouredThematicBreakRenderer : NormalizeObjectRenderer<ThematicBreakBlock>
{
    protected override void Write(NormalizeRenderer renderer, ThematicBreakBlock obj)
    {
        renderer.Write(new string(obj.ThematicChar, obj.ThematicCharCount));

        renderer.FinishBlock(renderer.Options.EmptyLineAfterThematicBreak);
    }
}

public class DiscordFlavouredQuoteBlockRenderer : NormalizeObjectRenderer<QuoteBlock>
{
    protected override void Write(NormalizeRenderer renderer, QuoteBlock obj)
    {
        renderer.EnsureLine();

        renderer.Write("<blockquote>");
        renderer.WriteChildren(obj);
        renderer.Write("</blockquote>");

        renderer.EnsureLine();
    }
}

// The idea here eventually is making this display somewhat nicely in discord, but it isn't much of an issue.
// Comments don't put tables in themselves frequently
// public class DiscordFlavouredTableRenderer : NormalizeObjectRenderer<Table>
// {
//     protected override void Write(NormalizeRenderer renderer, Table obj)
//     {
//         throw new NotImplementedException();
//     }
// }