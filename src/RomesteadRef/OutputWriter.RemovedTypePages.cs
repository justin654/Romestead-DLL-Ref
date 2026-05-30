using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private static string BuildRemovedTypePage(
        RemovedTypePageEntry removedTypePage,
        IReadOnlyDictionary<string, string> typeLinks,
        string? diffLabel)
    {
        var assembly = removedTypePage.Assembly;
        var type = removedTypePage.Type;
        var builder = new StringBuilder();
        AppendDocumentStart(
            builder,
            $"{type.FullName} - Romestead Assembly Reference",
            "../../style.css",
            "../../",
            "types",
            includeDiff: true,
            inlineScript: BuildCopyScript());
        builder.AppendLine($"<p class=\"breadcrumb\"><a href=\"../../index.html\">Catalog</a> / <a href=\"../../assemblies/{Encode(GetAssemblyFileName(assembly.Name))}\">{Encode(assembly.Name)}</a></p>");
        builder.AppendLine("<div class=\"title-row\">");
        builder.AppendLine($"<h1>{Encode(type.FullName)}</h1>");
        builder.AppendLine($"<button class=\"copy-button\" type=\"button\" data-copy=\"{Encode(type.FullName)}\">Copy type</button>");
        builder.AppendLine($"<button class=\"copy-button\" type=\"button\" data-copy=\"{Encode(BuildTypeMarkdown(assembly, type))}\">Copy markdown</button>");
        builder.AppendLine($"<a class=\"button\" href=\"{Encode(GetTypeJsonFileName(type))}\">Type JSON</a>");
        builder.AppendLine("<span class=\"change-badge change-removed\">removed</span>");
        builder.AppendLine("</div>");
        builder.AppendLine($"<p class=\"lede\">Last known snapshot for this type before {Encode(diffLabel ?? "the latest compared patch")}.</p>");
        builder.AppendLine($"<p class=\"notice\">This type is no longer present in the current scan. The page is preserved from the baseline snapshot so older patches, docs, and Harmony targets remain traceable.</p>");

        builder.AppendLine("<section class=\"panel\">");
        builder.AppendLine("<div class=\"member-summary\">");
        builder.AppendLine(MetricCard("Methods", type.Methods.Count.ToString("N0")));
        builder.AppendLine(MetricCard("Properties", type.Properties.Count.ToString("N0")));
        builder.AppendLine(MetricCard("Fields", type.Fields.Count.ToString("N0")));
        builder.AppendLine(MetricCard("Events", type.Events.Count.ToString("N0")));
        builder.AppendLine("</div>");
        builder.AppendLine("<details class=\"debug-details\">");
        builder.AppendLine("<summary>Type metadata</summary>");
        builder.AppendLine("<ul class=\"list compact\">");
        builder.AppendLine($"<li><span>Assembly</span><code>{Encode(assembly.Name)}</code></li>");
        builder.AppendLine($"<li><span>Namespace</span><code>{Encode(type.Namespace)}</code></li>");
        if (!string.IsNullOrWhiteSpace(type.BaseType))
        {
            builder.AppendLine($"<li><span>Base type</span><code>{LinkSignature(type.BaseType!, typeLinks, "../../")}</code></li>");
        }
        if (type.Interfaces.Count > 0)
        {
            builder.AppendLine($"<li><span>Interfaces</span><code>{LinkSignature(string.Join(", ", type.Interfaces), typeLinks, "../../")}</code></li>");
        }
        builder.AppendLine($"<li><span>Hash</span><code>{Encode(type.Hash)}</code></li>");
        builder.AppendLine("</ul>");
        builder.AppendLine("</details>");
        builder.AppendLine("</section>");

        if (type.Fields.Count > 0)
        {
            builder.AppendLine("<section class=\"panel\">");
            builder.AppendLine("<h2>Fields</h2>");
            builder.AppendLine("<table class=\"reference-table\"><thead><tr><th>Name</th><th>Visibility</th><th>Signature</th></tr></thead><tbody>");
            foreach (var field in type.Fields)
            {
                var fieldAnchor = BuildAnchor(field.Id);
                builder.AppendLine($"<tr class=\"member-entry\" data-member-kind=\"field\" data-member-visibility=\"{Encode(field.Visibility)}\" data-patch-target=\"false\" data-member-text=\"{Encode($"{field.Name} {field.Signature} {field.Type}")}\" id=\"{Encode(fieldAnchor)}\"><td>{BuildMemberTitle(field.Name, field.Signature, "removed", fieldAnchor)}</td><td>{Encode(field.Visibility)}</td><td><code>{LinkSignature(field.Signature, typeLinks, "../../")}</code></td></tr>");
            }
            builder.AppendLine("</tbody></table>");
            builder.AppendLine("</section>");
        }

        if (type.Properties.Count > 0)
        {
            builder.AppendLine("<section class=\"panel\">");
            builder.AppendLine("<h2>Properties</h2>");
            builder.AppendLine("<table class=\"reference-table\"><thead><tr><th>Name</th><th>Visibility</th><th>Signature</th></tr></thead><tbody>");
            foreach (var property in type.Properties)
            {
                var propertyAnchor = BuildAnchor(property.Id);
                builder.AppendLine($"<tr class=\"member-entry\" data-member-kind=\"property\" data-member-visibility=\"{Encode(property.Visibility)}\" data-patch-target=\"false\" data-member-text=\"{Encode($"{property.Name} {property.Signature} {property.Type}")}\" id=\"{Encode(propertyAnchor)}\"><td>{BuildMemberTitle(property.Name, property.Signature, "removed", propertyAnchor)}</td><td>{Encode(property.Visibility)}</td><td><code>{LinkSignature(property.Signature, typeLinks, "../../")}</code></td></tr>");
            }
            builder.AppendLine("</tbody></table>");
            builder.AppendLine("</section>");
        }

        if (type.Methods.Count > 0)
        {
            builder.AppendLine("<section class=\"panel\">");
            builder.AppendLine("<h2>Methods</h2>");
            foreach (var method in type.Methods)
            {
                var methodAnchor = BuildAnchor(method.Id);
                builder.AppendLine($"<article class=\"member-entry method\" data-member-kind=\"method\" data-member-visibility=\"{Encode(method.Visibility)}\" data-patch-target=\"{(method.BodyHash is null ? "false" : "true")}\" data-member-text=\"{Encode(method.Signature)}\" id=\"{Encode(methodAnchor)}\">");
                builder.AppendLine("<div class=\"method-heading\">");
                builder.AppendLine($"<h3><code>{Encode(method.Name)}</code></h3>");
                builder.AppendLine($"<a class=\"permalink\" href=\"#{Encode(methodAnchor)}\" aria-label=\"Permalink\">#</a>");
                builder.AppendLine($"<button class=\"copy-button\" type=\"button\" data-copy=\"{Encode(method.Signature)}\">Copy signature</button>");
                builder.AppendLine($"<span class=\"change-badge change-removed\">removed</span>");
                builder.AppendLine("</div>");
                builder.AppendLine($"<p><code>{LinkSignature(method.Signature, typeLinks, "../../")}</code></p>");
                builder.AppendLine("</article>");
            }
            builder.AppendLine("</section>");
        }

        AppendDocumentEnd(builder, $"Removed type preserved from baseline snapshot", "../../", "diff.html", "about.html", "site-manifest.json");
        return builder.ToString();
    }

    private static string BuildRemovedMembersSection(
        IReadOnlyList<RemovedMemberEntry>? removedMethods,
        IReadOnlyList<RemovedMemberEntry>? removedProperties,
        IReadOnlyList<RemovedMemberEntry>? removedFields,
        string? diffLabel)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"panel\">");
        builder.AppendLine("<h2>Removed in latest patch</h2>");
        builder.AppendLine($"<p class=\"muted\">These members existed in the baseline snapshot and were removed in {Encode(diffLabel ?? "the latest compared patch")}.</p>");
        if ((removedFields?.Count ?? 0) > 0)
        {
            builder.AppendLine(BuildRemovedMembersTable("Fields", removedFields!));
        }
        if ((removedProperties?.Count ?? 0) > 0)
        {
            builder.AppendLine(BuildRemovedMembersTable("Properties", removedProperties!));
        }
        if ((removedMethods?.Count ?? 0) > 0)
        {
            builder.AppendLine(BuildRemovedMembersTable("Methods", removedMethods!));
        }
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildRemovedMembersTable(string title, IReadOnlyList<RemovedMemberEntry> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"<h3>{Encode(title)}</h3>");
        builder.AppendLine("<table class=\"reference-table\"><thead><tr><th>Name</th><th>Visibility</th><th>Signature</th></tr></thead><tbody>");
        foreach (var entry in entries.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            var anchor = $"removed-{BuildAnchor(entry.Id)}";
            builder.AppendLine($"<tr class=\"member-entry\" data-member-kind=\"{Encode(entry.Kind)}\" data-member-visibility=\"{Encode(entry.Visibility)}\" data-patch-target=\"false\" data-member-text=\"{Encode($"{entry.Name} {entry.Signature}")}\" id=\"{Encode(anchor)}\"><td>{BuildMemberTitle(entry.Name, entry.Signature, entry.ChangeKind, anchor)}</td><td>{Encode(entry.Visibility)}</td><td><code>{Encode(entry.Signature)}</code></td></tr>");
        }
        builder.AppendLine("</tbody></table>");
        return builder.ToString();
    }
}
