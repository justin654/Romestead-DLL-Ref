using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private sealed record DiffTableRow(
        string ChangeKind,
        string AssemblyName,
        string TypeId,
        string Member,
        string HashChange);

    private static IReadOnlyDictionary<string, string> BuildTypeLinkIndex(
        CatalogSnapshot snapshot,
        IReadOnlyList<RemovedTypePageEntry> removedTypePages)
    {
        var links = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var assembly in snapshot.Assemblies)
        {
            foreach (var type in assembly.Types)
            {
                var link = $"types/{GetDirectorySafeName(assembly.Name)}/{GetTypeFileName(type)}";
                links.TryAdd(type.FullName, link);
            }
        }

        foreach (var removedTypePage in removedTypePages)
        {
            links.TryAdd(removedTypePage.Type.FullName, removedTypePage.Link);
        }

        return links;
    }

    private static CatalogPageContext BuildPageContext(
        CatalogSnapshot snapshot,
        CatalogSnapshot? baselineSnapshot,
        CatalogDiff? diff,
        IReadOnlyDictionary<string, string> typeLinks)
    {
        var typeEntries = snapshot.Assemblies
            .SelectMany(assembly => assembly.Types.Select(type => new CatalogTypeEntry(assembly, type)))
            .ToArray();
        var methodReferenceLinks = new Dictionary<string, MethodPageEntry>(StringComparer.Ordinal);
        var callers = new Dictionary<string, List<MethodPageEntry>>(StringComparer.Ordinal);
        var typeReferences = new Dictionary<string, List<CatalogTypeEntry>>(StringComparer.Ordinal);
        var derivedTypes = new Dictionary<string, List<CatalogTypeEntry>>(StringComparer.Ordinal);
        var interfaceImplementors = new Dictionary<string, List<CatalogTypeEntry>>(StringComparer.Ordinal);

        foreach (var entry in typeEntries)
        {
            var typeLink = typeLinks[entry.Type.FullName];
            if (!string.IsNullOrWhiteSpace(entry.Type.BaseType) && typeLinks.ContainsKey(entry.Type.BaseType))
            {
                AddGrouped(derivedTypes, entry.Type.BaseType!, entry);
            }

            foreach (var interfaceName in entry.Type.Interfaces.Where(typeLinks.ContainsKey))
            {
                AddGrouped(interfaceImplementors, interfaceName, entry);
            }

            foreach (var method in entry.Type.Methods)
            {
                var methodEntry = new MethodPageEntry(
                    entry.Assembly,
                    entry.Type,
                    method,
                    $"{typeLink}#{BuildAnchor(method.Id)}");
                methodReferenceLinks.TryAdd(BuildMethodReferenceKey(entry.Type, method), methodEntry);
            }
        }

        foreach (var entry in typeEntries)
        {
            foreach (var referencedTypeName in FindReferencedCatalogTypes(entry.Type, typeLinks.Keys))
            {
                if (!string.Equals(referencedTypeName, entry.Type.FullName, StringComparison.Ordinal))
                {
                    AddGrouped(typeReferences, referencedTypeName, entry);
                }
            }

            foreach (var method in entry.Type.Methods)
            {
                var callerLink = $"{typeLinks[entry.Type.FullName]}#{BuildAnchor(method.Id)}";
                var caller = new MethodPageEntry(entry.Assembly, entry.Type, method, callerLink);
                foreach (var calledMethod in method.CalledMethods)
                {
                    if (methodReferenceLinks.ContainsKey(calledMethod))
                    {
                        AddGrouped(callers, calledMethod, caller);
                    }
                }
            }
        }

        return new CatalogPageContext(
            typeLinks,
            methodReferenceLinks,
            NormalizeGroups(callers),
            NormalizeGroups(typeReferences),
            NormalizeGroups(derivedTypes),
            NormalizeGroups(interfaceImplementors),
            diff is not null,
            BuildTypeChangeMap(diff?.TypeChanges),
            BuildMemberChangeMap(diff?.MethodChanges),
            BuildMemberChangeMap(diff?.PropertyChanges),
            BuildMemberChangeMap(diff?.FieldChanges),
            BuildRemovedMemberMap(diff?.MethodChanges, baselineSnapshot, "method"),
            BuildRemovedMemberMap(diff?.PropertyChanges, baselineSnapshot, "property"),
            BuildRemovedMemberMap(diff?.FieldChanges, baselineSnapshot, "field"),
            GetDiffDisplayLabel(diff));
    }

    private static IReadOnlyList<RemovedTypePageEntry> BuildRemovedTypePages(
        CatalogSnapshot? baselineSnapshot,
        CatalogDiff? diff)
    {
        if (baselineSnapshot is null || diff is null)
        {
            return [];
        }

        var baselineTypes = baselineSnapshot.Assemblies
            .SelectMany(assembly => assembly.Types.Select(type => new { Assembly = assembly, Type = type }))
            .ToDictionary(
                entry => BuildCatalogKey(entry.Assembly.Name, entry.Type.Id),
                entry => entry,
                StringComparer.Ordinal);

        return diff.TypeChanges
            .Where(change => string.Equals(change.ChangeKind, "removed", StringComparison.OrdinalIgnoreCase))
            .Select(change =>
            {
                var baselineEntry = baselineTypes[BuildCatalogKey(change.AssemblyName, change.TypeId)];
                var link = $"types/{GetDirectorySafeName(baselineEntry.Assembly.Name)}/{GetTypeFileName(baselineEntry.Type)}";
                return new RemovedTypePageEntry(baselineEntry.Assembly, baselineEntry.Type, change, link);
            })
            .OrderBy(entry => entry.Assembly.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Type.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> FindReferencedCatalogTypes(TypeCatalog type, IEnumerable<string> catalogTypeNames)
    {
        var haystack = string.Join(
            " ",
            type.BaseType,
            string.Join(" ", type.Interfaces),
            string.Join(" ", type.Fields.Select(field => field.Signature)),
            string.Join(" ", type.Properties.Select(property => property.Signature)),
            string.Join(" ", type.Events.Select(eventInfo => eventInfo.Signature)),
            string.Join(" ", type.Methods.Select(method => method.Signature)));

        foreach (var typeName in catalogTypeNames)
        {
            if (haystack.Contains(typeName, StringComparison.Ordinal))
            {
                yield return typeName;
            }
        }
    }

    private static Dictionary<string, string> BuildTypeChangeMap(IReadOnlyList<TypeChange>? changes) =>
        changes is null
            ? []
            : changes.ToDictionary(change => BuildCatalogKey(change.AssemblyName, change.TypeId), change => change.ChangeKind, StringComparer.Ordinal);

    private static Dictionary<string, string> BuildMemberChangeMap(IReadOnlyList<MemberChange>? changes) =>
        changes is null
            ? []
            : changes.ToDictionary(change => BuildCatalogKey(change.AssemblyName, change.TypeId, change.MemberId), change => change.ChangeKind, StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, IReadOnlyList<RemovedMemberEntry>> BuildRemovedMemberMap(
        IReadOnlyList<MemberChange>? changes,
        CatalogSnapshot? baselineSnapshot,
        string memberKind)
    {
        if (changes is null || baselineSnapshot is null)
        {
            return new Dictionary<string, IReadOnlyList<RemovedMemberEntry>>(StringComparer.Ordinal);
        }

        var baselineTypes = baselineSnapshot.Assemblies
            .SelectMany(assembly => assembly.Types.Select(type => new { Assembly = assembly, Type = type }))
            .ToDictionary(
                entry => BuildCatalogKey(entry.Assembly.Name, entry.Type.Id),
                entry => entry.Type,
                StringComparer.Ordinal);

        var grouped = new Dictionary<string, List<RemovedMemberEntry>>(StringComparer.Ordinal);

        foreach (var change in changes.Where(change => string.Equals(change.ChangeKind, "removed", StringComparison.OrdinalIgnoreCase)))
        {
            if (!baselineTypes.TryGetValue(BuildCatalogKey(change.AssemblyName, change.TypeId), out var baselineType))
            {
                continue;
            }

            RemovedMemberEntry? entry = memberKind switch
            {
                "method" => baselineType.Methods
                    .Where(method => string.Equals(method.Id, change.MemberId, StringComparison.Ordinal))
                    .Select(method => new RemovedMemberEntry(memberKind, method.Id, method.Name, method.Signature, method.Visibility, change.ChangeKind))
                    .FirstOrDefault(),
                "property" => baselineType.Properties
                    .Where(property => string.Equals(property.Id, change.MemberId, StringComparison.Ordinal))
                    .Select(property => new RemovedMemberEntry(memberKind, property.Id, property.Name, property.Signature, property.Visibility, change.ChangeKind))
                    .FirstOrDefault(),
                "field" => baselineType.Fields
                    .Where(field => string.Equals(field.Id, change.MemberId, StringComparison.Ordinal))
                    .Select(field => new RemovedMemberEntry(memberKind, field.Id, field.Name, field.Signature, field.Visibility, change.ChangeKind))
                    .FirstOrDefault(),
                _ => null
            };

            if (entry is null)
            {
                continue;
            }

            var typeKey = BuildCatalogKey(change.AssemblyName, change.TypeId);
            if (!grouped.TryGetValue(typeKey, out var list))
            {
                list = [];
                grouped[typeKey] = list;
            }

            list.Add(entry);
        }

        return grouped.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyList<RemovedMemberEntry>)entry.Value,
            StringComparer.Ordinal);
    }

    private static string? GetDiffDisplayLabel(CatalogDiff? diff)
    {
        if (diff?.Metadata is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(diff.Metadata.PatchLabel))
        {
            return diff.Metadata.PatchLabel;
        }

        return diff.Metadata.NewGeneratedAtUtc.ToString("u");
    }

    private static string DescribeDiff(CatalogDiff diff)
    {
        if (diff.Metadata is null)
        {
            return "Comparison between the baseline snapshot and the current scan.";
        }

        var patchLabel = !string.IsNullOrWhiteSpace(diff.Metadata.PatchLabel)
            ? diff.Metadata.PatchLabel
            : "Latest compared patch";
        var baseline = !string.IsNullOrWhiteSpace(diff.Metadata.BaselineSnapshotLabel)
            ? diff.Metadata.BaselineSnapshotLabel
            : "baseline snapshot";

        return $"{patchLabel} | compared against {baseline} | {diff.Metadata.OldGeneratedAtUtc:u} -> {diff.Metadata.NewGeneratedAtUtc:u}";
    }

    private static Dictionary<string, IReadOnlyList<T>> NormalizeGroups<T>(Dictionary<string, List<T>> groups) =>
        groups.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyList<T>)entry.Value,
            StringComparer.Ordinal);

    private static void AddGrouped<T>(Dictionary<string, List<T>> groups, string key, T value)
    {
        if (!groups.TryGetValue(key, out var values))
        {
            values = [];
            groups[key] = values;
        }

        values.Add(value);
    }

    private static string BuildCatalogKey(string assemblyName, string typeId) =>
        $"{assemblyName}::{typeId}";

    private static string BuildCatalogKey(string assemblyName, string typeId, string memberId) =>
        $"{assemblyName}::{typeId}::{memberId}";

    private static string BuildMethodReferenceKey(TypeCatalog type, MethodCatalog method)
    {
        var genericSuffix = method.GenericArity > 0 ? $"`{method.GenericArity}" : "";
        return $"{type.FullName}::{method.Name}{genericSuffix}({string.Join(", ", method.Parameters.Select(parameter => parameter.Type))})";
    }

    private static string BuildHarmonyTarget(TypeCatalog type, MethodCatalog method) =>
        $"{type.FullName}::{method.Name}({string.Join(", ", method.Parameters.Select(parameter => parameter.Type))})";

    private static string BuildTypeMarkdown(AssemblyCatalog assembly, TypeCatalog type)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {type.FullName}");
        builder.AppendLine();
        builder.AppendLine($"- Assembly: `{assembly.Name}`");
        builder.AppendLine($"- Namespace: `{type.Namespace}`");
        builder.AppendLine($"- Kind: `{type.Visibility} {type.Kind}`");
        if (!string.IsNullOrWhiteSpace(type.BaseType))
        {
            builder.AppendLine($"- Base type: `{type.BaseType}`");
        }
        if (type.Interfaces.Count > 0)
        {
            builder.AppendLine($"- Interfaces: {string.Join(", ", type.Interfaces.Select(interfaceName => $"`{interfaceName}`"))}");
        }
        builder.AppendLine($"- Members: {type.Methods.Count} methods, {type.Properties.Count} properties, {type.Fields.Count} fields, {type.Events.Count} events");

        if (type.Methods.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Public Methods");
            foreach (var method in type.Methods.Where(method => method.Visibility == "public").Take(40))
            {
                builder.AppendLine($"- `{method.Signature}`");
            }
        }

        return builder.ToString();
    }

    private static string LinkMethodReference(
        string reference,
        IReadOnlyDictionary<string, MethodPageEntry> methodReferenceLinks,
        string rootPrefix)
    {
        if (methodReferenceLinks.TryGetValue(reference, out var entry))
        {
            return $"<a href=\"{Encode(rootPrefix + entry.Link)}\">{Encode(reference)}</a>";
        }

        return Encode(reference);
    }

    private static string BuildMemberTitle(string name, string copyValue, string? changeKind, string anchor)
    {
        var builder = new StringBuilder();
        builder.Append("<span class=\"member-name-cell\">");
        builder.Append($"<code>{Encode(name)}</code>");
        builder.Append($"<a class=\"permalink\" href=\"#{Encode(anchor)}\" aria-label=\"Permalink\">#</a>");
        builder.Append($"<button class=\"copy-button\" type=\"button\" data-copy=\"{Encode(copyValue)}\">Copy</button>");
        if (!string.IsNullOrWhiteSpace(changeKind))
        {
            builder.Append($"<span class=\"change-badge change-{Encode(changeKind)}\">{Encode(changeKind)}</span>");
        }
        builder.Append("</span>");
        return builder.ToString();
    }

    private static string LinkSignature(string signature, IReadOnlyDictionary<string, string> typeLinks, string rootPrefix)
    {
        var builder = new StringBuilder();
        var lastIndex = 0;

        foreach (Match match in TypeTokenRegex().Matches(signature))
        {
            builder.Append(Encode(signature[lastIndex..match.Index]));
            var token = match.Value;
            if (typeLinks.TryGetValue(token, out var link))
            {
                builder.Append($"<a href=\"{Encode(rootPrefix + link)}\">{Encode(token)}</a>");
            }
            else
            {
                builder.Append(Encode(token));
            }

            lastIndex = match.Index + match.Length;
        }

        builder.Append(Encode(signature[lastIndex..]));
        return builder.ToString();
    }
}
