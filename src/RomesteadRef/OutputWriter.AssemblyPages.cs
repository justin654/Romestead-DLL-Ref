using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private static string BuildAssemblyPage(
        CatalogSnapshot snapshot,
        AssemblyCatalog assembly,
        bool includeDiff,
        IReadOnlyList<RemovedTypePageEntry> removedTypes,
        string? diffLabel)
    {
        var builder = new StringBuilder();
        AppendDocumentStart(builder, $"{assembly.Name} - Romestead Assembly Reference", "../style.css", "../", "assemblies", includeDiff);
        builder.AppendLine($"<p class=\"breadcrumb\"><a href=\"../index.html\">Catalog</a> / Assemblies</p>");
        builder.AppendLine($"<h1>{Encode(assembly.Name)}</h1>");
        builder.AppendLine($"<p class=\"lede\">Version {Encode(assembly.Version)} | {assembly.TypeCount} types | {assembly.MethodCount} methods</p>");
        builder.AppendLine("<section class=\"panel\">");
        builder.AppendLine("<h2>Assembly metadata</h2>");
        builder.AppendLine("<ul class=\"list\">");
        builder.AppendLine($"<li><strong>Path:</strong> <code>{Encode(assembly.FilePath)}</code></li>");
        builder.AppendLine($"<li><strong>MVID:</strong> <code>{Encode(assembly.ModuleVersionId)}</code></li>");
        builder.AppendLine($"<li><strong>Hash:</strong> <code>{Encode(assembly.Hash)}</code></li>");
        builder.AppendLine("</ul>");
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"panel\">");
        builder.AppendLine("<h2>Namespaces</h2>");
        foreach (var ns in assembly.Namespaces)
        {
            builder.AppendLine($"<h3>{Encode(ns.Name)}</h3>");
            builder.AppendLine("<ul class=\"type-list\">");
            foreach (var type in ns.TypeIds.Select(typeId => assembly.Types.First(entry => entry.Id == typeId)))
            {
                builder.AppendLine(
                    $"<li><a href=\"../types/{Encode(GetDirectorySafeName(assembly.Name))}/{Encode(GetTypeFileName(type))}\">{Encode(type.DisplayName)}</a><span>{type.Methods.Count} methods</span></li>");
            }

            builder.AppendLine("</ul>");
        }

        builder.AppendLine("</section>");

        if (removedTypes.Count > 0)
        {
            builder.AppendLine("<section class=\"panel\">");
            builder.AppendLine($"<h2>Removed types</h2><p class=\"muted\">Last seen before {Encode(diffLabel ?? "the latest compared patch")}.</p>");
            builder.AppendLine("<ul class=\"type-list\">");
            foreach (var removedType in removedTypes.OrderBy(entry => entry.Type.FullName, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine(
                    $"<li><a href=\"../{Encode(removedType.Link)}\">{Encode(removedType.Type.DisplayName)}</a><span><span class=\"change-badge change-removed\">removed</span></span></li>");
            }
            builder.AppendLine("</ul>");
            builder.AppendLine("</section>");
        }

        if (assembly.References.Count > 0)
        {
            builder.AppendLine("<section class=\"panel\">");
            builder.AppendLine("<h2>Assembly references</h2>");
            builder.AppendLine("<ul class=\"list\">");
            foreach (var reference in assembly.References)
            {
                builder.AppendLine($"<li><code>{Encode(reference)}</code></li>");
            }
            builder.AppendLine("</ul>");
            builder.AppendLine("</section>");
        }

        AppendDocumentEnd(builder, "Assembly reference", "../", "about.html", "README.md", "site-manifest.json");
        return builder.ToString();
    }
}
