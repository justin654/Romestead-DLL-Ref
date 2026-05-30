namespace RomesteadRef;

public sealed record CatalogSnapshot(CatalogMetadata Metadata, IReadOnlyList<AssemblyCatalog> Assemblies);

public sealed record CatalogMetadata(
    DateTime GeneratedAtUtc,
    string BaseDirectory,
    IReadOnlyList<string> InputRoots,
    IReadOnlyList<string> AssemblyFilters,
    bool IncludeSystemAssemblies,
    bool IncludeCompilerGenerated,
    int AssemblyCount,
    int TypeCount,
    int MethodCount,
    int PropertyCount,
    int FieldCount);

public sealed record AssemblyCatalog(
    string Name,
    string Version,
    string FilePath,
    string ModuleVersionId,
    string Hash,
    IReadOnlyList<string> References,
    IReadOnlyList<NamespaceCatalog> Namespaces,
    IReadOnlyList<TypeCatalog> Types,
    int TypeCount,
    int MethodCount,
    int PropertyCount,
    int FieldCount);

public sealed record NamespaceCatalog(
    string Name,
    IReadOnlyList<string> TypeIds,
    int TypeCount,
    int MethodCount);

public sealed record TypeCatalog(
    string Id,
    string Name,
    string DisplayName,
    string FullName,
    string Namespace,
    string Kind,
    string Visibility,
    bool IsAbstract,
    bool IsSealed,
    string? BaseType,
    IReadOnlyList<string> Interfaces,
    IReadOnlyList<SourceLocation> SourceLocations,
    IReadOnlyList<FieldCatalog> Fields,
    IReadOnlyList<PropertyCatalog> Properties,
    IReadOnlyList<EventCatalog> Events,
    IReadOnlyList<MethodCatalog> Methods,
    string Hash);

public sealed record FieldCatalog(
    string Id,
    string Name,
    string Type,
    string Signature,
    string Visibility,
    bool IsStatic,
    bool IsLiteral,
    string Hash);

public sealed record PropertyCatalog(
    string Id,
    string Name,
    string Type,
    string Signature,
    string Visibility,
    bool HasGetter,
    bool HasSetter,
    string Hash);

public sealed record EventCatalog(
    string Id,
    string Name,
    string Type,
    string Signature,
    string Visibility,
    string Hash);

public sealed record MethodCatalog(
    string Id,
    string Name,
    string Signature,
    string Visibility,
    string ReturnType,
    IReadOnlyList<ParameterCatalog> Parameters,
    bool IsStatic,
    bool IsAbstract,
    bool IsVirtual,
    bool IsConstructor,
    int GenericArity,
    int IlSize,
    string? BodyHash,
    string Hash,
    IReadOnlyList<string> CalledMethods,
    IReadOnlyList<string> StringLiterals,
    SourceLocation? Source);

public sealed record ParameterCatalog(
    string Name,
    string Type,
    bool IsOut,
    bool IsOptional);

public sealed record SourceLocation(string DocumentPath, int? StartLine);

public sealed record SkippedAssembly(string Path, string Reason);

public sealed record CatalogBuildOptions
{
    public required string BaseDirectory { get; init; }
    public required IReadOnlyList<string> InputRoots { get; init; }
    public IReadOnlyList<string> AssemblyNameFilters { get; init; } = [];
    public IReadOnlyList<string> ExactAssemblyNameFilters { get; init; } = [];
    public bool IncludeSystemAssemblies { get; init; }
    public bool IncludeCompilerGenerated { get; init; }
}

public sealed record CatalogBuildResult(
    CatalogSnapshot Snapshot,
    IReadOnlyList<string> IncludedAssemblyPaths,
    IReadOnlyList<SkippedAssembly> SkippedAssemblies);

public sealed record CatalogDiff(
    DiffSummary Summary,
    IReadOnlyList<AssemblyChange> AssemblyChanges,
    IReadOnlyList<TypeChange> TypeChanges,
    IReadOnlyList<MemberChange> MethodChanges,
    IReadOnlyList<MemberChange> PropertyChanges,
    IReadOnlyList<MemberChange> FieldChanges,
    DiffMetadata? Metadata = null);

public sealed record DiffMetadata(
    string? PatchLabel,
    string? BaselineSnapshotLabel,
    DateTime OldGeneratedAtUtc,
    DateTime NewGeneratedAtUtc);

public sealed record DiffSummary(
    int AddedAssemblies,
    int RemovedAssemblies,
    int ChangedAssemblies,
    int AddedTypes,
    int RemovedTypes,
    int ChangedTypes,
    int AddedMethods,
    int RemovedMethods,
    int ChangedMethods,
    int AddedProperties,
    int RemovedProperties,
    int ChangedProperties,
    int AddedFields,
    int RemovedFields,
    int ChangedFields);

public sealed record AssemblyChange(
    string ChangeKind,
    string Name,
    string? OldVersion,
    string? NewVersion,
    string? OldHash,
    string? NewHash);

public sealed record TypeChange(
    string ChangeKind,
    string AssemblyName,
    string TypeId,
    string DisplayName,
    string? OldHash,
    string? NewHash);

public sealed record MemberChange(
    string ChangeKind,
    string AssemblyName,
    string TypeId,
    string MemberId,
    string DisplayName,
    string? OldHash,
    string? NewHash);
