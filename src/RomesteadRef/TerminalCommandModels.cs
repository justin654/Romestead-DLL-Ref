namespace RomesteadRef;

public sealed record TerminalCommandCatalog(
    TerminalCommandMetadata Metadata,
    IReadOnlyList<TerminalCommandEntry> Commands);

public sealed record TerminalCommandMetadata(
    DateTime GeneratedAtUtc,
    string? SourceAssemblyPath,
    string? SourceAssemblySha256,
    string RegistryType,
    string RegistryMethod,
    string RegistryStorage,
    string DispatchPath,
    string AutocompletePath,
    string? RegistryBodyHash,
    int CommandCount);

public sealed record TerminalCommandEntry(
    int Order,
    string Name,
    string DotName,
    string Usage,
    string Summary,
    string Category,
    bool IsModdingUseful,
    string HandlerMethod,
    string? HandlerBodyHash,
    string SuggestionKind,
    string? SuggestionMethod,
    string? SuggestionBodyHash,
    string? AutocompleteSource,
    string SourceOffset,
    string ChangeHash);

public sealed record TerminalCommandDiff(
    TerminalCommandDiffSummary Summary,
    IReadOnlyList<TerminalCommandEntry> Added,
    IReadOnlyList<TerminalCommandEntry> Removed,
    IReadOnlyList<TerminalCommandChange> Changed,
    DateTime OldGeneratedAtUtc,
    DateTime NewGeneratedAtUtc);

public sealed record TerminalCommandDiffSummary(
    int Added,
    int Removed,
    int Changed);

public sealed record TerminalCommandChange(
    string Name,
    TerminalCommandEntry OldCommand,
    TerminalCommandEntry NewCommand);
