using System.Xml.Linq;

namespace MessageFlow.Core.Text;

/// <summary>
/// Reads the visible lines of a PowerPoint (DrawingML) text paragraph. A lyric slide often keeps
/// a whole verse in one &lt;a:p&gt; and breaks its lines with Shift+Enter, which is stored as
/// &lt;a:br/&gt; between the runs, not as text. Reading only the &lt;a:t&gt; runs glued every line
/// of such a verse into one long line on the projector.
/// </summary>
public static class PresentationParagraphText
{
    public static readonly XNamespace DrawingNamespace = "http://schemas.openxmlformats.org/drawingml/2006/main";

    /// <summary>
    /// The lines of <paramref name="paragraph"/>, split at every &lt;a:br/&gt; (and at a vertical tab,
    /// which some exporters write inside the text for the same soft break). <paramref name="joinRuns"/>
    /// joins the runs of one line; the default concatenates them as PowerPoint displays them.
    /// Lines are returned untrimmed and may be empty, so callers keep their own cleaning rules.
    /// </summary>
    public static IReadOnlyList<string> GetLines(XElement paragraph, Func<IReadOnlyList<string>, string>? joinRuns = null)
    {
        joinRuns ??= static runs => string.Concat(runs);

        var lines = new List<string>();
        var runs = new List<string>();
        foreach (var element in paragraph.Descendants())
        {
            if (element.Name == DrawingNamespace + "br")
            {
                lines.Add(joinRuns(runs));
                runs.Clear();
                continue;
            }

            if (element.Name == DrawingNamespace + "tab")
            {
                runs.Add(" ");
                continue;
            }

            if (element.Name != DrawingNamespace + "t")
            {
                continue;
            }

            var segments = element.Value.Split('\v');
            for (var i = 0; i < segments.Length; i++)
            {
                if (i > 0)
                {
                    lines.Add(joinRuns(runs));
                    runs.Clear();
                }

                runs.Add(segments[i]);
            }
        }

        lines.Add(joinRuns(runs));
        return lines;
    }

    /// <summary>
    /// Every non-blank line of every paragraph in a slide, in document order.
    /// </summary>
    public static List<string> GetSlideLines(XDocument slide, Func<IReadOnlyList<string>, string>? joinRuns = null)
    {
        return slide
            .Descendants(DrawingNamespace + "p")
            .SelectMany(paragraph => GetLines(paragraph, joinRuns))
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }
}
