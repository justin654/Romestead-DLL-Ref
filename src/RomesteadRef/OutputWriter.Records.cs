using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private sealed record CatalogTypeEntry(AssemblyCatalog Assembly, TypeCatalog Type);

    private sealed record MethodPageEntry(
        AssemblyCatalog Assembly,
        TypeCatalog Type,
        MethodCatalog Method,
        string Link);

    private sealed record CatalogPageContext(
        IReadOnlyDictionary<string, string> TypeLinks,
        IReadOnlyDictionary<string, MethodPageEntry> MethodReferenceLinks,
        IReadOnlyDictionary<string, IReadOnlyList<MethodPageEntry>> Callers,
        IReadOnlyDictionary<string, IReadOnlyList<CatalogTypeEntry>> TypeReferences,
        IReadOnlyDictionary<string, IReadOnlyList<CatalogTypeEntry>> DerivedTypes,
        IReadOnlyDictionary<string, IReadOnlyList<CatalogTypeEntry>> InterfaceImplementors,
        bool HasDiff,
        IReadOnlyDictionary<string, string> TypeChanges,
        IReadOnlyDictionary<string, string> MethodChanges,
        IReadOnlyDictionary<string, string> PropertyChanges,
        IReadOnlyDictionary<string, string> FieldChanges,
        IReadOnlyDictionary<string, IReadOnlyList<RemovedMemberEntry>> RemovedMethods,
        IReadOnlyDictionary<string, IReadOnlyList<RemovedMemberEntry>> RemovedProperties,
        IReadOnlyDictionary<string, IReadOnlyList<RemovedMemberEntry>> RemovedFields,
        string? DiffLabel);

    private sealed record RemovedTypePageEntry(
        AssemblyCatalog Assembly,
        TypeCatalog Type,
        TypeChange Change,
        string Link);

    private sealed record RemovedMemberEntry(
        string Kind,
        string Id,
        string Name,
        string Signature,
        string Visibility,
        string ChangeKind);

    private sealed record TopicDefinition(string Name, IReadOnlyList<string> Tokens);

    private sealed record TopicGroup(string Name, IReadOnlyList<CatalogTypeEntry> Types);

    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)+")]
    private static partial Regex TypeTokenRegex();
}
