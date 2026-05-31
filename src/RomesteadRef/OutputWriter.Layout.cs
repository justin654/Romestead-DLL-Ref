using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private static void AppendDocumentStart(
        StringBuilder builder,
        string title,
        string stylesheetPath,
        string rootPrefix,
        string activePage,
        bool includeDiff = false,
        string? externalScriptPath = null,
        string? inlineScript = null)
    {
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head>");
        builder.AppendLine("<meta charset=\"utf-8\">");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine($"<title>{Encode(title)}</title>");
        builder.AppendLine($"<link rel=\"stylesheet\" href=\"{Encode(stylesheetPath)}\">");
        if (!string.IsNullOrWhiteSpace(externalScriptPath))
        {
            builder.AppendLine($"<script defer src=\"{Encode(externalScriptPath)}\"></script>");
        }

        var pageScript = string.IsNullOrWhiteSpace(inlineScript)
            ? BuildJsonDownloadPromptScript()
            : $"{BuildJsonDownloadPromptScript()}{Environment.NewLine}{inlineScript}";
        if (!string.IsNullOrWhiteSpace(pageScript))
        {
            builder.AppendLine("<script defer>");
            builder.AppendLine(pageScript);
            builder.AppendLine("</script>");
        }

        builder.AppendLine("</head><body>");
        builder.AppendLine("<div class=\"site-frame\">");
        builder.AppendLine("<header class=\"topbar\">");
        builder.AppendLine($"<a class=\"brand\" href=\"{Encode(rootPrefix)}index.html\">Romestead Assembly Reference</a>");
        builder.AppendLine("<nav class=\"topnav\" aria-label=\"Primary navigation\">");
        builder.AppendLine(BuildTopNavLink(rootPrefix, "index.html", "Home", activePage, "home"));
        builder.AppendLine(BuildTopNavLink(rootPrefix, "guide.html", "Guide", activePage, "guide"));
        builder.AppendLine(BuildTopNavLink(rootPrefix, "namespaces.html", "Namespaces", activePage, "namespaces"));
        builder.AppendLine(BuildTopNavLink(rootPrefix, "topics.html", "Topics", activePage, "topics"));
        builder.AppendLine(BuildTopNavLink(rootPrefix, "about.html", "About", activePage, "about"));
        if (includeDiff)
        {
            builder.AppendLine(BuildTopNavLink(rootPrefix, "diff.html", "Diff", activePage, "diff"));
        }
        builder.AppendLine("</nav>");
        builder.AppendLine("</header>");
        builder.AppendLine("<main class=\"page\">");
    }

    private static string BuildTopNavLink(
        string rootPrefix,
        string href,
        string label,
        string activePage,
        string pageId)
    {
        var activeAttributes = string.Equals(activePage, pageId, StringComparison.OrdinalIgnoreCase)
            ? " class=\"active\" aria-current=\"page\""
            : "";
        return $"<a{activeAttributes} href=\"{Encode(rootPrefix)}{Encode(href)}\">{Encode(label)}</a>";
    }

    private static void AppendDocumentEnd(StringBuilder builder, string footerLabel, string rootPrefix, params string[] links)
    {
        builder.AppendLine(BuildFooter(footerLabel, rootPrefix, links));
        builder.AppendLine("</main></div></body></html>");
    }

    private static string BuildFooter(string label, string rootPrefix, params string[] links)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<footer class=\"footer site-footer\">");
        builder.AppendLine($"<span>{Encode(label)}</span>");
        foreach (var link in links)
        {
            builder.AppendLine($"<a href=\"{Encode(rootPrefix)}{Encode(link)}\">{Encode(Path.GetFileName(link))}</a>");
        }
        builder.AppendLine("</footer>");
        return builder.ToString();
    }
}
