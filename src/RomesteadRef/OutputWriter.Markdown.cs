using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    public static string RenderDiffMarkdown(CatalogDiff diff)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Catalog Diff");
        builder.AppendLine();
        builder.AppendLine($"- Assemblies: +{diff.Summary.AddedAssemblies} / -{diff.Summary.RemovedAssemblies} / ~{diff.Summary.ChangedAssemblies}");
        builder.AppendLine($"- Types: +{diff.Summary.AddedTypes} / -{diff.Summary.RemovedTypes} / ~{diff.Summary.ChangedTypes}");
        builder.AppendLine($"- Methods: +{diff.Summary.AddedMethods} / -{diff.Summary.RemovedMethods} / ~{diff.Summary.ChangedMethods}");
        builder.AppendLine($"- Properties: +{diff.Summary.AddedProperties} / -{diff.Summary.RemovedProperties} / ~{diff.Summary.ChangedProperties}");
        builder.AppendLine($"- Fields: +{diff.Summary.AddedFields} / -{diff.Summary.RemovedFields} / ~{diff.Summary.ChangedFields}");

        AppendSection(builder, "Changed Assemblies", diff.AssemblyChanges.Select(change =>
            $"- `{change.ChangeKind}` `{change.Name}` {FormatHashChange(change.OldHash, change.NewHash)}"));
        AppendSection(builder, "Changed Types", diff.TypeChanges.Select(change =>
            $"- `{change.ChangeKind}` `{change.AssemblyName}` `{change.TypeId}` {FormatHashChange(change.OldHash, change.NewHash)}"));
        AppendSection(builder, "Changed Methods", diff.MethodChanges.Select(change =>
            $"- `{change.ChangeKind}` `{change.AssemblyName}` `{change.TypeId}` `{change.DisplayName}` {FormatHashChange(change.OldHash, change.NewHash)}"));
        AppendSection(builder, "Changed Properties", diff.PropertyChanges.Select(change =>
            $"- `{change.ChangeKind}` `{change.AssemblyName}` `{change.TypeId}` `{change.DisplayName}` {FormatHashChange(change.OldHash, change.NewHash)}"));
        AppendSection(builder, "Changed Fields", diff.FieldChanges.Select(change =>
            $"- `{change.ChangeKind}` `{change.AssemblyName}` `{change.TypeId}` `{change.DisplayName}` {FormatHashChange(change.OldHash, change.NewHash)}"));

        return builder.ToString();
    }

    public static string RenderApiOpportunityMarkdown(ApiOpportunityReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# API Opportunities");
        builder.AppendLine();
        builder.AppendLine($"Generated {report.GeneratedAtUtc:u}");

        foreach (var surface in report.Surfaces)
        {
            var topExternalTypes = surface.ExternalTypes
                .Where(type => type.Candidates.Count > 0)
                .Take(3)
                .ToArray();

            builder.AppendLine();
            builder.AppendLine($"## {surface.Title}");
            builder.AppendLine();
            builder.AppendLine($"- Interface: `{surface.InterfaceDisplayName}`");
            if (surface.CurrentApiMembers.Count > 0)
            {
                builder.AppendLine($"- Current API members: {string.Join(", ", surface.CurrentApiMembers.Select(member => $"`{member.Name}`"))}");
            }
            if (surface.Implementations.Count > 0)
            {
                builder.AppendLine($"- Implementations: {string.Join(", ", surface.Implementations.Select(implementation => $"`{implementation.DisplayName}`"))}");
            }
            if (topExternalTypes.Length > 0)
            {
                builder.AppendLine($"- Best next seams: {string.Join(", ", topExternalTypes.Select(type => $"`{type.DisplayName}`"))}");
            }

            if (topExternalTypes.Length > 0)
            {
                builder.AppendLine("- Recommended expansion targets:");
                foreach (var externalType in topExternalTypes)
                {
                    builder.AppendLine($"  - `{externalType.AssemblyName}` `{externalType.TypeId}`");
                    foreach (var candidate in externalType.Candidates.Take(3))
                    {
                        builder.AppendLine($"    - game: `{candidate.GameMethodSignature}`");
                        foreach (var related in candidate.RelatedApiMembers.Take(2))
                        {
                            builder.AppendLine($"      - related api: `{related}`");
                        }
                        foreach (var suggestion in candidate.SuggestedApiMembers.Take(3))
                        {
                            builder.AppendLine($"      - suggest: `{suggestion}`");
                        }
                    }
                }
            }
        }

        return builder.ToString();
    }

    private static void AppendSection(StringBuilder builder, string title, IEnumerable<string> entries)
    {
        var materialized = entries.Take(200).ToArray();
        if (materialized.Length == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine($"## {title}");
        builder.AppendLine();
        foreach (var entry in materialized)
        {
            builder.AppendLine(entry);
        }
    }
}
