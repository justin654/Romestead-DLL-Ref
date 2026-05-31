using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private static string BuildIndexPage(CatalogBuildResult buildResult, CatalogDiff? diff, ApiOpportunityReport apiOpportunityReport)
    {
        var snapshot = buildResult.Snapshot;
        var topicGroups = BuildTopicGroups(snapshot).Where(group => group.Types.Count > 0).ToArray();
        var topNamespaces = snapshot.Assemblies
            .SelectMany(assembly => assembly.Namespaces.Select(ns => (Assembly: assembly, Namespace: ns)))
            .OrderByDescending(entry => entry.Namespace.TypeCount)
            .ThenBy(entry => entry.Namespace.Name, StringComparer.OrdinalIgnoreCase)
            .Take(24)
            .ToArray();

        var builder = new StringBuilder();
        AppendDocumentStart(
            builder,
            "Romestead Assembly Reference",
            "style.css",
            "",
            "home",
            diff is not null,
            "search-index.js",
            $"{BuildSearchScript()}{Environment.NewLine}{BuildCopyScript()}");
        builder.AppendLine("<header class=\"site-header\">");
        builder.AppendLine("<div>");
        builder.AppendLine("<h1>Romestead Assembly Reference</h1>");
        builder.AppendLine($"<p class=\"lede\">{snapshot.Metadata.AssemblyCount} assemblies, {snapshot.Metadata.TypeCount:N0} types, {snapshot.Metadata.MethodCount:N0} methods. See <a href=\"site-manifest.json\">site manifest</a> for build metadata.</p>");
        builder.AppendLine("<p class=\"notice\">Unofficial generated reference for modding. Use public members first; treat private/internal members as patch targets that can change between updates.</p>");
        builder.AppendLine("</div>");
        builder.AppendLine("<nav class=\"header-actions\">");
        builder.AppendLine("<a class=\"button primary-button\" href=\"guide.html\">Read the guide</a>");
        builder.AppendLine("<a class=\"button\" href=\"commands.html\">Terminal Commands</a>");
        builder.AppendLine("<a class=\"button\" href=\"snapshot.json\">Snapshot JSON</a>");
        builder.AppendLine("<a class=\"button\" href=\"site-manifest.json\">Manifest</a>");
        if (diff is not null)
        {
            builder.AppendLine("<a class=\"button\" href=\"diff.html\">Patch Diff</a>");
        }
        builder.AppendLine("</nav>");
        builder.AppendLine("</header>");

        builder.AppendLine("<section class=\"search-panel\">");
        builder.AppendLine("<label for=\"searchBox\">Search classes, namespaces, methods, properties, and fields</label>");
        builder.AppendLine("<input id=\"searchBox\" type=\"search\" placeholder=\"InventoryApplyEngine, Candide.World, ItemFlag, GetPingMilliseconds\">");
        builder.AppendLine("<p class=\"search-help\">Press <kbd>/</kbd> to focus search. Results link directly to matching types and members.</p>");
        builder.AppendLine("<div class=\"search-filters\" aria-label=\"Search filters\">");
        builder.AppendLine("<label><input class=\"search-kind\" type=\"checkbox\" value=\"assembly\" checked> Assemblies</label>");
        builder.AppendLine("<label><input class=\"search-kind\" type=\"checkbox\" value=\"namespace\" checked> Namespaces</label>");
        builder.AppendLine("<label><input class=\"search-kind\" type=\"checkbox\" value=\"type\" checked> Types</label>");
        builder.AppendLine("<label><input class=\"search-kind\" type=\"checkbox\" value=\"method\" checked> Methods</label>");
        builder.AppendLine("<label><input class=\"search-kind\" type=\"checkbox\" value=\"property\" checked> Properties</label>");
        builder.AppendLine("<label><input class=\"search-kind\" type=\"checkbox\" value=\"field\" checked> Fields</label>");
        builder.AppendLine("<label><input id=\"searchPublicOnly\" type=\"checkbox\"> Public only</label>");
        builder.AppendLine("</div>");
        builder.AppendLine("<div id=\"searchResults\" class=\"search-results\"></div>");
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"panel\">");
        builder.AppendLine("<div class=\"section-heading\"><h2>Start Here</h2><a href=\"guide.html\">Open modder guide</a></div>");
        builder.AppendLine("<div class=\"guide-grid\">");
        builder.AppendLine("<article><h3>Find a target</h3><p>Search by gameplay term, namespace, class name, field, or method signature. Start broad, then filter to public members if you want safer integration points.</p></article>");
        builder.AppendLine("<article><h3>Open the type page</h3><p>Type pages collect metadata, relationships, fields, properties, methods, caller/callee hints, and per-type JSON exports in one place.</p></article>");
        builder.AppendLine("<article><h3>Copy the patch target</h3><p>Use copy buttons for full signatures and Harmony-style target strings. Check method metadata before patching overloads or private members.</p></article>");
        builder.AppendLine("<article><h3>Use dot commands</h3><p>The Commands page lists terminal commands, handlers, autocomplete sources, and debug notes.</p></article>");
        builder.AppendLine("<article><h3>Check the diff</h3><p>When a patch ships, use the diff page and change badges to quickly find moved, added, removed, or changed members.</p></article>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"panel docs-panel\">");
        builder.AppendLine("<div class=\"section-heading\"><h2>Reference Scope</h2><a href=\"about.html\">Read notes and limitations</a></div>");
        builder.AppendLine("<div class=\"docs-grid\">");
        builder.AppendLine("<article><h3>What this is</h3><p>A generated reference for classes, interfaces, fields, properties, and methods discovered in the scanned Romestead assemblies.</p></article>");
        builder.AppendLine("<article><h3>For modding</h3><p>Type pages include copyable signatures, Harmony-style targets, caller/callee hints, JSON exports, and latest-patch change badges.</p></article>");
        builder.AppendLine("<article><h3>Stability</h3><p>Private and internal members are included for patch authors, but they are not stable API and may move between game updates.</p></article>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"panel\">");
        builder.AppendLine("<div class=\"section-heading\"><h2>Assemblies</h2><a href=\"namespaces.html\">Browse all namespaces</a></div>");
        builder.AppendLine("<div class=\"assembly-grid\">");
        foreach (var assembly in snapshot.Assemblies)
        {
            var topNamespaceNames = assembly.Namespaces
                .OrderByDescending(ns => ns.TypeCount)
                .Take(4)
                .Select(ns => ns.Name)
                .ToArray();
            builder.AppendLine("<article class=\"assembly-card\">");
            builder.AppendLine($"<h3><a href=\"assemblies/{Encode(GetAssemblyFileName(assembly.Name))}\">{Encode(assembly.Name)}</a></h3>");
            builder.AppendLine($"<p>{assembly.TypeCount:N0} types, {assembly.MethodCount:N0} methods, {assembly.PropertyCount:N0} properties, {assembly.FieldCount:N0} fields</p>");
            builder.AppendLine($"<p class=\"muted\">{Encode(string.Join(", ", topNamespaceNames))}</p>");
            builder.AppendLine("</article>");
        }
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"panel\">");
        builder.AppendLine("<div class=\"section-heading\"><h2>Topic Index</h2><a href=\"topics.html\">Open full topic index</a></div>");
        builder.AppendLine("<div class=\"topic-grid\">");
        foreach (var group in topicGroups.Take(8))
        {
            var sampleTypes = group.Types.Take(3).Select(entry => entry.Type.DisplayName);
            builder.AppendLine("<article class=\"topic-card\">");
            builder.AppendLine($"<h3><a href=\"topics.html#{Encode(BuildAnchor(group.Name))}\">{Encode(group.Name)}</a></h3>");
            builder.AppendLine($"<p>{group.Types.Count:N0} matching types</p>");
            builder.AppendLine($"<p class=\"muted\">{Encode(string.Join(", ", sampleTypes))}</p>");
            builder.AppendLine("</article>");
        }
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"panel\">");
        builder.AppendLine("<div class=\"section-heading\"><h2>Largest Namespaces</h2><a href=\"namespaces.html\">View all</a></div>");
        builder.AppendLine("<table class=\"reference-table\"><thead><tr><th>Namespace</th><th>Assembly</th><th>Types</th><th>Methods</th></tr></thead><tbody>");
        foreach (var entry in topNamespaces)
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<td><a href=\"namespaces.html#{Encode(BuildNamespaceAnchor(entry.Assembly.Name, entry.Namespace.Name))}\"><code>{Encode(entry.Namespace.Name)}</code></a></td>");
            builder.AppendLine($"<td><a href=\"assemblies/{Encode(GetAssemblyFileName(entry.Assembly.Name))}\">{Encode(entry.Assembly.Name)}</a></td>");
            builder.AppendLine($"<td>{entry.Namespace.TypeCount:N0}</td>");
            builder.AppendLine($"<td>{entry.Namespace.MethodCount:N0}</td>");
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody></table>");
        builder.AppendLine("</section>");

        if (diff is not null)
        {
            builder.AppendLine("<section class=\"panel compact-panel\">");
            builder.AppendLine("<div class=\"section-heading\"><h2>Patch Diff</h2><a href=\"diff.html\">Open diff</a></div>");
            builder.AppendLine("<ul class=\"stat-list\">");
            builder.AppendLine($"<li><span>Types</span><strong>+{diff.Summary.AddedTypes} / -{diff.Summary.RemovedTypes} / ~{diff.Summary.ChangedTypes}</strong></li>");
            builder.AppendLine($"<li><span>Methods</span><strong>+{diff.Summary.AddedMethods} / -{diff.Summary.RemovedMethods} / ~{diff.Summary.ChangedMethods}</strong></li>");
            builder.AppendLine($"<li><span>Fields</span><strong>+{diff.Summary.AddedFields} / -{diff.Summary.RemovedFields} / ~{diff.Summary.ChangedFields}</strong></li>");
            builder.AppendLine("</ul>");
            builder.AppendLine("</section>");
        }

        if (buildResult.SkippedAssemblies.Count > 0)
        {
            builder.AppendLine("<section class=\"panel compact-panel\">");
            builder.AppendLine("<h2>Skipped assemblies</h2>");
            builder.AppendLine("<ul class=\"list\">");
            foreach (var skipped in buildResult.SkippedAssemblies.OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"<li><code>{Encode(Path.GetFileName(skipped.Path))}</code> <span>{Encode(skipped.Reason)}</span></li>");
            }
            builder.AppendLine("</ul>");
            builder.AppendLine("</section>");
        }

        AppendDocumentEnd(builder, "Generated reference package", "", "about.html", "README.md", "site-manifest.json");
        return builder.ToString();
    }
}
