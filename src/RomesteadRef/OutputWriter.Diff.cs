using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private static string BuildDiffPage(CatalogDiff diff)
    {
        var builder = new StringBuilder();
        AppendDocumentStart(builder, "Patch Diff - Romestead Assembly Reference", "style.css", "", "diff", includeDiff: true);
        builder.AppendLine("<p class=\"breadcrumb\"><a href=\"index.html\">Catalog</a></p>");
        builder.AppendLine("<h1>Catalog Diff</h1>");
        builder.AppendLine($"<p class=\"lede\">{Encode(DescribeDiff(diff))}</p>");
        builder.AppendLine("<section class=\"metrics\">");
        builder.AppendLine(MetricCard("Assemblies", $"+{diff.Summary.AddedAssemblies} / -{diff.Summary.RemovedAssemblies} / ~{diff.Summary.ChangedAssemblies}"));
        builder.AppendLine(MetricCard("Types", $"+{diff.Summary.AddedTypes} / -{diff.Summary.RemovedTypes} / ~{diff.Summary.ChangedTypes}"));
        builder.AppendLine(MetricCard("Methods", $"+{diff.Summary.AddedMethods} / -{diff.Summary.RemovedMethods} / ~{diff.Summary.ChangedMethods}"));
        builder.AppendLine("</section>");
        builder.AppendLine(BuildDiffTable("Assemblies", diff.AssemblyChanges.Select(change =>
            new DiffTableRow(change.ChangeKind, change.Name, "", "", FormatHashChange(change.OldHash, change.NewHash)))));
        builder.AppendLine(BuildDiffTable("Types", diff.TypeChanges.Select(change =>
            new DiffTableRow(change.ChangeKind, change.AssemblyName, change.TypeId, "", FormatHashChange(change.OldHash, change.NewHash)))));
        builder.AppendLine(BuildDiffTable("Methods", diff.MethodChanges.Select(change =>
            new DiffTableRow(change.ChangeKind, change.AssemblyName, change.TypeId, change.DisplayName, FormatHashChange(change.OldHash, change.NewHash)))));
        builder.AppendLine(BuildDiffTable("Properties", diff.PropertyChanges.Select(change =>
            new DiffTableRow(change.ChangeKind, change.AssemblyName, change.TypeId, change.DisplayName, FormatHashChange(change.OldHash, change.NewHash)))));
        builder.AppendLine(BuildDiffTable("Fields", diff.FieldChanges.Select(change =>
            new DiffTableRow(change.ChangeKind, change.AssemblyName, change.TypeId, change.DisplayName, FormatHashChange(change.OldHash, change.NewHash)))));
        AppendDocumentEnd(builder, "Patch diff report", "", "index.html", "about.html", "site-manifest.json");
        return builder.ToString();
    }

    private static string BuildDiffTable(string title, IEnumerable<DiffTableRow> rows)
    {
        var materialized = rows.Take(250).ToArray();
        if (materialized.Length == 0)
        {
            return "";
        }

        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"panel\">");
        builder.AppendLine($"<h2>{Encode(title)}</h2>");
        builder.AppendLine("<table class=\"diff-table\"><thead><tr><th>Change</th><th>Assembly</th><th>Type</th><th>Member</th><th>Hash</th></tr></thead><tbody>");
        foreach (var row in materialized)
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<td><code class=\"diff-kind\">{Encode(row.ChangeKind)}</code></td>");
            builder.AppendLine($"<td><code>{Encode(row.AssemblyName)}</code></td>");
            builder.AppendLine($"<td><code>{Encode(row.TypeId)}</code></td>");
            builder.AppendLine($"<td><code>{Encode(row.Member)}</code></td>");
            builder.AppendLine($"<td><code>{Encode(row.HashChange)}</code></td>");
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody></table>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }
}
