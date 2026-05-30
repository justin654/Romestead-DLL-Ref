using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private static string BuildNamespacesPage(CatalogSnapshot snapshot)
    {
        var namespaceEntries = snapshot.Assemblies
            .SelectMany(assembly => assembly.Namespaces.Select(ns => (Assembly: assembly, Namespace: ns)))
            .OrderBy(entry => entry.Assembly.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Namespace.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var builder = new StringBuilder();
        AppendDocumentStart(builder, "Namespaces - Romestead Assembly Reference", "style.css", "", "namespaces");
        builder.AppendLine("<p class=\"breadcrumb\"><a href=\"index.html\">Catalog</a></p>");
        builder.AppendLine("<header class=\"site-header\"><div><h1>Namespaces</h1>");
        builder.AppendLine($"<p class=\"lede\">{namespaceEntries.Length:N0} namespaces across {snapshot.Metadata.AssemblyCount} assemblies.</p></div></header>");

        foreach (var assemblyGroup in namespaceEntries.GroupBy(entry => entry.Assembly.Name, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine("<section class=\"panel\">");
            builder.AppendLine($"<div class=\"section-heading\"><h2><a href=\"assemblies/{Encode(GetAssemblyFileName(assemblyGroup.Key))}\">{Encode(assemblyGroup.Key)}</a></h2><span>{assemblyGroup.Count():N0} namespaces</span></div>");
            foreach (var entry in assemblyGroup)
            {
                builder.AppendLine($"<details class=\"namespace\" id=\"{Encode(BuildNamespaceAnchor(entry.Assembly.Name, entry.Namespace.Name))}\">");
                builder.AppendLine($"<summary><code>{Encode(entry.Namespace.Name)}</code><span>{entry.Namespace.TypeCount:N0} types, {entry.Namespace.MethodCount:N0} methods</span></summary>");
                builder.AppendLine("<ul class=\"type-list\">");
                foreach (var type in entry.Namespace.TypeIds
                             .Select(typeId => entry.Assembly.Types.First(type => type.Id == typeId))
                             .OrderBy(type => type.DisplayName, StringComparer.OrdinalIgnoreCase))
                {
                    builder.AppendLine($"<li><a href=\"types/{Encode(GetDirectorySafeName(entry.Assembly.Name))}/{Encode(GetTypeFileName(type))}\">{Encode(type.DisplayName)}</a><span>{Encode(type.Kind)}</span></li>");
                }
                builder.AppendLine("</ul>");
                builder.AppendLine("</details>");
            }
            builder.AppendLine("</section>");
        }

        AppendDocumentEnd(builder, "Namespace browser", "", "about.html", "README.md", "site-manifest.json");
        return builder.ToString();
    }

    private static string BuildTopicsPage(CatalogSnapshot snapshot)
    {
        var topicGroups = BuildTopicGroups(snapshot).Where(group => group.Types.Count > 0).ToArray();

        var builder = new StringBuilder();
        AppendDocumentStart(builder, "Topics - Romestead Assembly Reference", "style.css", "", "topics");
        builder.AppendLine("<p class=\"breadcrumb\"><a href=\"index.html\">Catalog</a></p>");
        builder.AppendLine("<header class=\"site-header\"><div><h1>Topic Index</h1>");
        builder.AppendLine("<p class=\"lede\">Heuristic groups based on type names, namespaces, and member names.</p></div></header>");

        builder.AppendLine("<section class=\"panel\"><div class=\"topic-grid\">");
        foreach (var group in topicGroups)
        {
            builder.AppendLine("<article class=\"topic-card\">");
            builder.AppendLine($"<h3><a href=\"#{Encode(BuildAnchor(group.Name))}\">{Encode(group.Name)}</a></h3>");
            builder.AppendLine($"<p>{group.Types.Count:N0} matching types</p>");
            builder.AppendLine("</article>");
        }
        builder.AppendLine("</div></section>");

        foreach (var group in topicGroups)
        {
            builder.AppendLine($"<section class=\"panel\" id=\"{Encode(BuildAnchor(group.Name))}\">");
            builder.AppendLine($"<div class=\"section-heading\"><h2>{Encode(group.Name)}</h2><span>{group.Types.Count:N0} types</span></div>");
            builder.AppendLine("<table class=\"reference-table\"><thead><tr><th>Type</th><th>Assembly</th><th>Namespace</th><th>Members</th></tr></thead><tbody>");
            foreach (var entry in group.Types
                         .OrderBy(entry => entry.Type.Namespace, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(entry => entry.Type.DisplayName, StringComparer.OrdinalIgnoreCase)
                         .Take(300))
            {
                builder.AppendLine("<tr>");
                builder.AppendLine($"<td><a href=\"types/{Encode(GetDirectorySafeName(entry.Assembly.Name))}/{Encode(GetTypeFileName(entry.Type))}\"><code>{Encode(entry.Type.DisplayName)}</code></a></td>");
                builder.AppendLine($"<td>{Encode(entry.Assembly.Name)}</td>");
                builder.AppendLine($"<td><code>{Encode(entry.Type.Namespace)}</code></td>");
                builder.AppendLine($"<td>{entry.Type.Methods.Count:N0} methods, {entry.Type.Fields.Count:N0} fields</td>");
                builder.AppendLine("</tr>");
            }
            builder.AppendLine("</tbody></table>");
            if (group.Types.Count > 300)
            {
                builder.AppendLine($"<p class=\"muted\">Showing first 300 of {group.Types.Count:N0} matches. Use search for narrower lookup.</p>");
            }
            builder.AppendLine("</section>");
        }

        AppendDocumentEnd(builder, "Topic browser", "", "about.html", "README.md", "site-manifest.json");
        return builder.ToString();
    }
}
