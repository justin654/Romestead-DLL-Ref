using System.Security.Cryptography;
using System.Text.Json;
using Mono.Cecil;

namespace RomesteadRef;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static int Main(string[] args)
    {
        try
        {
            var command = CommandArguments.Parse(args);
            return command.Name switch
            {
                "scan" or "wiki" => RunScan(command),
                "diff" => RunDiff(command),
                "commands" => RunCommands(command),
                "find" => RunFind(command),
                "inspect" => RunInspect(command),
                "calls" => RunCalls(command),
                "xref" => RunCrossReferences(command),
                "relate" => RunRelate(command),
                "help" or "--help" or "-h" => ShowHelp(),
                _ => Fail($"Unknown command '{command.Name}'. Run 'romestead-ref help' for usage.")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static int RunScan(CommandArguments command)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var outputDirectory = command.GetSingle("--out") ??
            Path.Combine(currentDirectory, "output", "latest");

        var roots = command.GetMany("--root");
        if (roots.Count == 0)
        {
            roots = [currentDirectory];
        }

        var resolvedRoots = roots.Select(path => Path.GetFullPath(path, currentDirectory)).ToArray();
        if (!command.HasFlag("--allow-modded"))
        {
            var cleanCheck = CheckRomesteadDllState(resolvedRoots);
            if (!cleanCheck.IsClean)
            {
                return Fail(cleanCheck.Message);
            }

            if (!string.IsNullOrWhiteSpace(cleanCheck.Message))
            {
                Console.WriteLine(cleanCheck.Message);
            }
        }

        var knownCleanPath = command.GetSingle("--known-clean");
        if (command.HasFlag("--require-known-clean") && string.IsNullOrWhiteSpace(knownCleanPath))
        {
            knownCleanPath = Path.Combine(currentDirectory, "known-clean-romestead.json");
        }

        if (!string.IsNullOrWhiteSpace(knownCleanPath))
        {
            var knownCleanCheck = CheckKnownCleanRomesteadDll(
                resolvedRoots,
                Path.GetFullPath(knownCleanPath, currentDirectory),
                command.GetSingle("--expected-manifest"));
            if (!knownCleanCheck.IsClean)
            {
                return Fail(knownCleanCheck.Message);
            }

            Console.WriteLine(knownCleanCheck.Message);
        }

        var buildResult = new CatalogBuilder().Build(new CatalogBuildOptions
        {
            BaseDirectory = currentDirectory,
            InputRoots = resolvedRoots,
            AssemblyNameFilters = command.GetMany("--assembly"),
            ExactAssemblyNameFilters = command.GetMany("--assembly-exact"),
            IncludeSystemAssemblies = command.HasFlag("--include-all-assemblies"),
            IncludeCompilerGenerated = command.HasFlag("--include-compiler-generated")
        });

        if (buildResult.Snapshot.Assemblies.Count == 0)
        {
            return Fail("No assemblies matched the requested scan.");
        }

        var baselinePath = command.GetSingle("--baseline");
        if (string.IsNullOrWhiteSpace(baselinePath))
        {
            var latestSnapshotPath = Path.Combine(outputDirectory, "snapshot.json");
            if (File.Exists(latestSnapshotPath))
            {
                baselinePath = latestSnapshotPath;
            }
        }

        CatalogSnapshot? baselineSnapshot = null;
        if (!string.IsNullOrWhiteSpace(baselinePath) && File.Exists(baselinePath))
        {
            baselineSnapshot = LoadSnapshot(Path.GetFullPath(baselinePath, currentDirectory));
        }

        OutputWriter.PrepareOutputDirectory(outputDirectory);
        var apiOpportunityReport = new ApiOpportunityAnalyzer().Analyze(buildResult.Snapshot);

        var snapshotPath = Path.Combine(outputDirectory, "snapshot.json");
        var historyDirectory = Path.Combine(outputDirectory, "history");
        Directory.CreateDirectory(historyDirectory);
        File.WriteAllText(snapshotPath, JsonSerializer.Serialize(buildResult.Snapshot, JsonOptions));

        var historyPath = Path.Combine(
            historyDirectory,
            $"snapshot-{buildResult.Snapshot.Metadata.GeneratedAtUtc:yyyyMMdd-HHmmss}.json");
        File.WriteAllText(historyPath, JsonSerializer.Serialize(buildResult.Snapshot, JsonOptions));

        CatalogDiff? diff = null;
        if (baselineSnapshot is not null)
        {
            diff = CatalogDiffEngine.Compare(baselineSnapshot, buildResult.Snapshot) with
            {
                Metadata = BuildDiffMetadata(
                    baselineSnapshot,
                    buildResult.Snapshot,
                    command.GetSingle("--patch-label"),
                    baselinePath)
            };
            File.WriteAllText(
                Path.Combine(outputDirectory, "diff.json"),
                JsonSerializer.Serialize(diff, JsonOptions));
            File.WriteAllText(
                Path.Combine(outputDirectory, "diff.md"),
                OutputWriter.RenderDiffMarkdown(diff));
        }

        OutputWriter.WriteCatalogSite(outputDirectory, buildResult, baselineSnapshot, diff, apiOpportunityReport);

        Console.WriteLine($"Wrote catalog: {Path.Combine(outputDirectory, "index.html")}");
        Console.WriteLine($"Wrote snapshot: {snapshotPath}");
        Console.WriteLine($"Wrote API opportunities: {Path.Combine(outputDirectory, "api-opportunities.html")}");
        Console.WriteLine($"Archived snapshot: {historyPath}");
        Console.WriteLine(
            $"Scanned {buildResult.Snapshot.Assemblies.Count} assemblies, {buildResult.Snapshot.Assemblies.Sum(assembly => assembly.TypeCount)} types, {buildResult.Snapshot.Assemblies.Sum(assembly => assembly.MethodCount)} methods.");

        if (diff is not null)
        {
            Console.WriteLine($"Wrote diff: {Path.Combine(outputDirectory, "diff.html")}");
            Console.WriteLine(
                $"Diff summary: +{diff.Summary.AddedTypes}/-{diff.Summary.RemovedTypes} types, +{diff.Summary.AddedMethods}/-{diff.Summary.RemovedMethods} methods, {diff.Summary.ChangedMethods} changed methods.");
        }

        foreach (var skipped in buildResult.SkippedAssemblies)
        {
            Console.WriteLine($"SKIP {Path.GetFileName(skipped.Path)}: {skipped.Reason}");
        }

        return 0;
    }

    private static int RunDiff(CommandArguments command)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var oldPath = command.GetSingle("--old");
        var newPath = command.GetSingle("--new");

        if (string.IsNullOrWhiteSpace(oldPath) || string.IsNullOrWhiteSpace(newPath))
        {
            return Fail("diff requires --old <snapshot.json> and --new <snapshot.json>.");
        }

        var oldSnapshot = LoadSnapshot(Path.GetFullPath(oldPath, currentDirectory));
        var newSnapshot = LoadSnapshot(Path.GetFullPath(newPath, currentDirectory));
        var diff = CatalogDiffEngine.Compare(oldSnapshot, newSnapshot) with
        {
            Metadata = BuildDiffMetadata(
                oldSnapshot,
                newSnapshot,
                command.GetSingle("--patch-label"),
                oldPath)
        };

        var outputDirectory = command.GetSingle("--out");
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            OutputWriter.PrepareOutputDirectory(outputDirectory);
            File.WriteAllText(
                Path.Combine(outputDirectory, "diff.json"),
                JsonSerializer.Serialize(diff, JsonOptions));
            File.WriteAllText(
                Path.Combine(outputDirectory, "diff.md"),
                OutputWriter.RenderDiffMarkdown(diff));
            OutputWriter.WriteDiffSite(outputDirectory, diff);
            Console.WriteLine($"Wrote diff bundle: {Path.Combine(outputDirectory, "diff.html")}");
        }
        else
        {
            Console.WriteLine(OutputWriter.RenderDiffMarkdown(diff));
        }

        return 0;
    }

    private static int RunCommands(CommandArguments command)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var outputDirectory = command.GetSingle("--out") ??
            Path.Combine(currentDirectory, "output", "commands");

        var roots = command.GetMany("--root");
        if (roots.Count == 0)
        {
            roots = [currentDirectory];
        }

        var resolvedRoots = roots.Select(path => Path.GetFullPath(path, currentDirectory)).ToArray();
        if (!command.HasFlag("--allow-modded"))
        {
            var cleanCheck = CheckRomesteadDllState(resolvedRoots);
            if (!cleanCheck.IsClean)
            {
                return Fail(cleanCheck.Message);
            }

            if (!string.IsNullOrWhiteSpace(cleanCheck.Message))
            {
                Console.WriteLine(cleanCheck.Message);
            }
        }

        var knownCleanPath = command.GetSingle("--known-clean");
        if (command.HasFlag("--require-known-clean") && string.IsNullOrWhiteSpace(knownCleanPath))
        {
            knownCleanPath = Path.Combine(currentDirectory, "known-clean-romestead.json");
        }

        if (!string.IsNullOrWhiteSpace(knownCleanPath))
        {
            var knownCleanCheck = CheckKnownCleanRomesteadDll(
                resolvedRoots,
                Path.GetFullPath(knownCleanPath, currentDirectory),
                command.GetSingle("--expected-manifest"));
            if (!knownCleanCheck.IsClean)
            {
                return Fail(knownCleanCheck.Message);
            }

            Console.WriteLine(knownCleanCheck.Message);
        }

        var buildResult = new CatalogBuilder().Build(new CatalogBuildOptions
        {
            BaseDirectory = currentDirectory,
            InputRoots = resolvedRoots,
            AssemblyNameFilters = command.GetMany("--assembly"),
            ExactAssemblyNameFilters = command.GetMany("--assembly-exact"),
            IncludeSystemAssemblies = command.HasFlag("--include-all-assemblies"),
            IncludeCompilerGenerated = command.HasFlag("--include-compiler-generated")
        });

        if (buildResult.Snapshot.Assemblies.Count == 0)
        {
            return Fail("No assemblies matched the requested command scan.");
        }

        var baselinePath = command.GetSingle("--old") ??
            command.GetSingle("--old-commands") ??
            command.GetSingle("--baseline");
        if (string.IsNullOrWhiteSpace(baselinePath))
        {
            var previousCommandsPath = Path.Combine(outputDirectory, "commands.json");
            if (File.Exists(previousCommandsPath))
            {
                baselinePath = previousCommandsPath;
            }
        }

        TerminalCommandCatalog? previousCommands = null;
        if (!string.IsNullOrWhiteSpace(baselinePath))
        {
            var resolvedBaselinePath = Path.GetFullPath(baselinePath, currentDirectory);
            if (!File.Exists(resolvedBaselinePath))
            {
                return Fail($"Command baseline not found: {resolvedBaselinePath}");
            }

            previousCommands = LoadTerminalCommands(resolvedBaselinePath);
        }

        var terminalCommands = new TerminalCommandAnalyzer().Analyze(
            buildResult.IncludedAssemblyPaths,
            buildResult.Snapshot.Metadata.GeneratedAtUtc);
        var terminalCommandDiff = TerminalCommandDiffEngine.Compare(previousCommands, terminalCommands);
        var commandHistoryPath = OutputWriter.WriteCommandsBundle(
            outputDirectory,
            terminalCommands,
            terminalCommandDiff,
            buildResult.Snapshot);

        Console.WriteLine($"Wrote commands: {Path.Combine(outputDirectory, "commands.html")} ({terminalCommands.Metadata.CommandCount} commands)");
        Console.WriteLine($"Archived commands: {commandHistoryPath}");

        if (terminalCommandDiff is not null)
        {
            Console.WriteLine(
                $"Command diff summary: +{terminalCommandDiff.Summary.Added}/-{terminalCommandDiff.Summary.Removed}/~{terminalCommandDiff.Summary.Changed} commands.");
        }

        foreach (var skipped in buildResult.SkippedAssemblies)
        {
            Console.WriteLine($"SKIP {Path.GetFileName(skipped.Path)}: {skipped.Reason}");
        }

        return 0;
    }

    private static int RunFind(CommandArguments command)
    {
        if (command.Positionals.Count == 0)
        {
            return Fail("find requires at least one search pattern.");
        }

        var snapshot = BuildSnapshotForSearch(command);
        var patterns = command.Positionals;

        foreach (var assembly in snapshot.Assemblies.OrderBy(assembly => assembly.Name, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var type in assembly.Types.Where(type =>
                         MatchesAny(type.FullName, patterns) ||
                         MatchesAny(type.DisplayName, patterns))
                     .OrderBy(type => type.FullName, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"{assembly.Name}: {type.FullName}");
            }

            foreach (var member in assembly.Types.SelectMany(type => type.Methods.Select(method => (type, method)))
                         .Where(entry =>
                             MatchesAny(entry.method.Name, patterns) ||
                             MatchesAny(entry.method.Signature, patterns))
                         .OrderBy(entry => $"{entry.type.FullName}.{entry.method.Name}", StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"{assembly.Name}: {member.type.FullName} :: {member.method.Signature}");
            }
        }

        return 0;
    }

    private static int RunInspect(CommandArguments command)
    {
        if (command.Positionals.Count == 0)
        {
            return Fail("inspect requires at least one search pattern.");
        }

        var snapshot = BuildSnapshotForSearch(command);
        var matches = snapshot.Assemblies
            .SelectMany(assembly => assembly.Types.Select(type => (assembly, type)))
            .Where(entry =>
                MatchesAny(entry.type.FullName, command.Positionals) ||
                MatchesAny(entry.type.DisplayName, command.Positionals))
            .OrderBy(entry => entry.type.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var (assembly, type) in matches)
        {
            Console.WriteLine($"TYPE {type.FullName}");
            Console.WriteLine($"  ASSEMBLY {assembly.Name}");
            Console.WriteLine($"  KIND {type.Kind}");
            Console.WriteLine($"  VISIBILITY {type.Visibility}");
            Console.WriteLine($"  BASE {type.BaseType ?? "<none>"}");

            if (type.Interfaces.Count > 0)
            {
                Console.WriteLine($"  INTERFACES {string.Join(", ", type.Interfaces)}");
            }

            foreach (var field in type.Fields.OrderBy(field => field.Name, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  F {field.Signature}");
            }

            foreach (var property in type.Properties.OrderBy(property => property.Name, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  P {property.Signature}");
            }

            foreach (var method in type.Methods.OrderBy(method => method.Name, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  M {method.Signature}");
            }
        }

        return 0;
    }

    private static int RunCalls(CommandArguments command)
    {
        if (command.Positionals.Count == 0)
        {
            return Fail("calls requires at least one search pattern.");
        }

        var snapshot = BuildSnapshotForSearch(command);

        foreach (var (assembly, type, method) in snapshot.Assemblies
                     .SelectMany(assembly => assembly.Types.SelectMany(type => type.Methods.Select(method => (assembly, type, method))))
                     .Where(entry =>
                         MatchesAny(entry.method.Name, command.Positionals) ||
                         MatchesAny(entry.method.Signature, command.Positionals))
                     .OrderBy(entry => $"{entry.type.FullName}.{entry.method.Name}", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"METHOD {type.FullName}.{method.Name}");
            Console.WriteLine($"  ASSEMBLY {assembly.Name}");
            Console.WriteLine($"  SIGNATURE {method.Signature}");
            Console.WriteLine($"  BODY {method.BodyHash ?? "<none>"}");

            foreach (var call in method.CalledMethods)
            {
                Console.WriteLine($"  CALL {call}");
            }

            foreach (var text in method.StringLiterals)
            {
                Console.WriteLine($"  STR \"{text}\"");
            }
        }

        return 0;
    }

    private static int RunCrossReferences(CommandArguments command)
    {
        if (command.Positionals.Count == 0)
        {
            return Fail("xref requires at least one search pattern.");
        }

        var snapshot = BuildSnapshotForSearch(command);

        foreach (var assembly in snapshot.Assemblies.OrderBy(assembly => assembly.Name, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var type in assembly.Types.OrderBy(type => type.FullName, StringComparer.OrdinalIgnoreCase))
            {
                var hits = new List<string>();

                foreach (var field in type.Fields.Where(field => MatchesAny(field.Signature, command.Positionals)))
                {
                    hits.Add($"F {field.Signature}");
                }

                foreach (var property in type.Properties.Where(property => MatchesAny(property.Signature, command.Positionals)))
                {
                    hits.Add($"P {property.Signature}");
                }

                foreach (var method in type.Methods.Where(method =>
                             MatchesAny(method.Signature, command.Positionals) ||
                             method.CalledMethods.Any(call => MatchesAny(call, command.Positionals)) ||
                             method.StringLiterals.Any(text => MatchesAny(text, command.Positionals))))
                {
                    hits.Add($"M {method.Signature}");
                }

                if (hits.Count == 0)
                {
                    continue;
                }

                Console.WriteLine($"TYPE {type.FullName}");
                Console.WriteLine($"  ASSEMBLY {assembly.Name}");
                foreach (var hit in hits.Distinct(StringComparer.Ordinal).OrderBy(hit => hit, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"  {hit}");
                }
            }
        }

        return 0;
    }

    private static CatalogSnapshot BuildSnapshotForSearch(CommandArguments command)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var roots = command.GetMany("--root");
        if (roots.Count == 0)
        {
            roots = [currentDirectory];
        }

        return new CatalogBuilder().Build(new CatalogBuildOptions
        {
            BaseDirectory = currentDirectory,
            InputRoots = roots.Select(path => Path.GetFullPath(path, currentDirectory)).ToArray(),
            AssemblyNameFilters = command.GetMany("--assembly"),
            ExactAssemblyNameFilters = command.GetMany("--assembly-exact"),
            IncludeSystemAssemblies = command.HasFlag("--include-all-assemblies"),
            IncludeCompilerGenerated = command.HasFlag("--include-compiler-generated")
        }).Snapshot;
    }

    private static int RunRelate(CommandArguments command)
    {
        if (command.Positionals.Count == 0)
        {
            return Fail("relate requires at least one search pattern.");
        }

        var snapshot = BuildSnapshotForSearch(command);
        var report = new ApiOpportunityAnalyzer().Analyze(snapshot);
        var patterns = command.Positionals;

        foreach (var surface in report.Surfaces)
        {
            var surfaceMatched =
                MatchesAny(surface.InterfaceDisplayName, patterns) ||
                MatchesAny(surface.Title, patterns) ||
                surface.Implementations.Any(implementation => MatchesAny(implementation.DisplayName, patterns));

            var currentMembers = surface.CurrentApiMembers
                .Where(member => MatchesAny(member.Signature, patterns) || MatchesAny(member.Name, patterns))
                .ToArray();
            var bridgeMembers = surface.BridgeMethods
                .Where(method =>
                    MatchesAny(method.MethodSignature, patterns) ||
                    method.ExternalCalls.Any(call => MatchesAny(call, patterns)))
                .ToArray();

            if (surfaceMatched && currentMembers.Length == 0)
            {
                currentMembers = surface.CurrentApiMembers.Take(6).ToArray();
            }

            if (surfaceMatched && bridgeMembers.Length == 0)
            {
                bridgeMembers = surface.BridgeMethods.Take(6).ToArray();
            }

            var matchedBridgeSignatures = bridgeMembers
                .Select(method => method.MethodSignature)
                .ToHashSet(StringComparer.Ordinal);
            var candidateMembers = surface.ExternalTypes
                .SelectMany(type => type.Candidates.Select(candidate => (type, candidate)))
                .Where(entry =>
                {
                    var directMatch =
                        MatchesAny(entry.candidate.GameMethodSignature, patterns) ||
                        entry.candidate.RelatedBridgeMethods.Any(method => MatchesAny(method, patterns)) ||
                        entry.candidate.SuggestedApiMembers.Any(suggestion => MatchesAny(suggestion, patterns));

                    if (directMatch)
                    {
                        return true;
                    }

                    if (currentMembers.Length == 0 || bridgeMembers.Length == 0)
                    {
                        return false;
                    }

                    return entry.candidate.RelatedApiMembers.Any(member => MatchesAny(member, patterns)) &&
                           entry.candidate.RelatedBridgeMethods.Any(method => matchedBridgeSignatures.Contains(method));
                })
                .ToArray();

            if (!surfaceMatched && currentMembers.Length == 0 && bridgeMembers.Length == 0 && candidateMembers.Length == 0)
            {
                continue;
            }

            Console.WriteLine($"SURFACE {surface.InterfaceDisplayName}");
            foreach (var member in currentMembers)
            {
                Console.WriteLine($"  API {member.Signature}");
            }

            foreach (var bridge in bridgeMembers)
            {
                Console.WriteLine($"  BRIDGE {bridge.TypeDisplayName} :: {bridge.MethodSignature}");
            }

            foreach (var (type, candidate) in candidateMembers)
            {
                Console.WriteLine($"  CANDIDATE {type.TypeId} :: {candidate.GameMethodSignature}");
                foreach (var related in candidate.RelatedApiMembers)
                {
                    Console.WriteLine($"    RELATED_API {related}");
                }
                foreach (var related in candidate.RelatedBridgeMethods)
                {
                    Console.WriteLine($"    RELATED_BRIDGE {related}");
                }
                foreach (var suggestion in candidate.SuggestedApiMembers)
                {
                    Console.WriteLine($"    SUGGEST {suggestion}");
                }
            }
        }

        return 0;
    }

    private static CatalogSnapshot LoadSnapshot(string path)
    {
        var snapshot = JsonSerializer.Deserialize<CatalogSnapshot>(File.ReadAllText(path));
        return snapshot ?? throw new InvalidOperationException($"Failed to deserialize snapshot '{path}'.");
    }

    private static TerminalCommandCatalog LoadTerminalCommands(string path)
    {
        var catalog = JsonSerializer.Deserialize<TerminalCommandCatalog>(File.ReadAllText(path));
        return catalog ?? throw new InvalidOperationException($"Failed to deserialize terminal command catalog '{path}'.");
    }

    private static DiffMetadata BuildDiffMetadata(
        CatalogSnapshot oldSnapshot,
        CatalogSnapshot newSnapshot,
        string? patchLabel,
        string? baselinePath) =>
        new(
            patchLabel,
            string.IsNullOrWhiteSpace(baselinePath) ? null : Path.GetFileName(baselinePath),
            oldSnapshot.Metadata.GeneratedAtUtc,
            newSnapshot.Metadata.GeneratedAtUtc);

    private static CleanDllCheckResult CheckRomesteadDllState(IReadOnlyList<string> resolvedRoots)
    {
        var romesteadPath = FindRomesteadDllPath(resolvedRoots);
        if (string.IsNullOrWhiteSpace(romesteadPath) || !File.Exists(romesteadPath))
        {
            return new CleanDllCheckResult(true, "CLEAN CHECK skipped: Romestead.dll was not found in the requested scan roots.");
        }

        var backupPath = $"{romesteadPath}.modloader-backup";
        var markers = FindModLoaderMarkers(romesteadPath);

        if (File.Exists(backupPath))
        {
            var activeHash = ComputeFileSha256(romesteadPath);
            var backupHash = ComputeFileSha256(backupPath);

            if (string.Equals(activeHash, backupHash, StringComparison.OrdinalIgnoreCase))
            {
                return new CleanDllCheckResult(
                    true,
                    $"CLEAN CHECK passed: {Path.GetFileName(romesteadPath)} matches {Path.GetFileName(backupPath)}.");
            }

            if (markers.Count > 0)
            {
                return new CleanDllCheckResult(
                    false,
                    $"Refusing to scan likely modded Romestead.dll. It differs from {Path.GetFileName(backupPath)} and contains mod-loader markers: {string.Join(", ", markers)}. Restore the clean DLL or rerun with --allow-modded.");
            }

            return new CleanDllCheckResult(
                false,
                $"Refusing to scan unknown Romestead.dll state. It differs from {Path.GetFileName(backupPath)}, but no mod-loader markers were found. Restore or verify the clean DLL before publishing, or rerun with --allow-modded for local analysis.");
        }

        if (markers.Count > 0)
        {
            return new CleanDllCheckResult(
                false,
                $"Refusing to scan likely modded Romestead.dll. Detected mod-loader markers: {string.Join(", ", markers)}. Restore the clean DLL or rerun with --allow-modded.");
        }

        return new CleanDllCheckResult(
            true,
            $"CLEAN CHECK passed: no mod-loader markers detected in {Path.GetFileName(romesteadPath)}.");
    }

    private static CleanDllCheckResult CheckKnownCleanRomesteadDll(
        IReadOnlyList<string> resolvedRoots,
        string knownCleanPath,
        string? expectedManifest)
    {
        var romesteadPath = FindRomesteadDllPath(resolvedRoots);
        if (string.IsNullOrWhiteSpace(romesteadPath) || !File.Exists(romesteadPath))
        {
            return new CleanDllCheckResult(false, "Known-clean verification failed: Romestead.dll was not found in the requested scan roots.");
        }

        if (!File.Exists(knownCleanPath))
        {
            return new CleanDllCheckResult(false, $"Known-clean verification failed: hash allowlist not found at '{knownCleanPath}'.");
        }

        var entries = JsonSerializer.Deserialize<List<KnownCleanRomesteadFile>>(
            File.ReadAllText(knownCleanPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        var fileInfo = new FileInfo(romesteadPath);
        var sha1 = ComputeFileSha1(romesteadPath);
        var candidates = entries
            .Where(entry => string.IsNullOrWhiteSpace(entry.File) || entry.File.Equals("Romestead.dll", StringComparison.OrdinalIgnoreCase))
            .Where(entry => string.IsNullOrWhiteSpace(expectedManifest) || string.Equals(entry.ManifestId, expectedManifest, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (candidates.Length == 0)
        {
            return new CleanDllCheckResult(
                false,
                string.IsNullOrWhiteSpace(expectedManifest)
                    ? $"Known-clean verification failed: no Romestead.dll entries exist in '{knownCleanPath}'."
                    : $"Known-clean verification failed: manifest {expectedManifest} is not listed in '{knownCleanPath}'.");
        }

        var match = candidates.FirstOrDefault(entry =>
            entry.Size == fileInfo.Length &&
            entry.Sha1.Equals(sha1, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return new CleanDllCheckResult(
                false,
                $"Known-clean verification failed: Romestead.dll size/SHA1 {fileInfo.Length}/{sha1} does not match {(string.IsNullOrWhiteSpace(expectedManifest) ? "any known clean entry" : $"manifest {expectedManifest}")}.");
        }

        return new CleanDllCheckResult(
            true,
            $"KNOWN CLEAN passed: {Path.GetFileName(romesteadPath)} matches {match.Label ?? match.ManifestId ?? "known clean entry"} ({match.Size}/{match.Sha1}).");
    }

    private static string? FindRomesteadDllPath(IReadOnlyList<string> resolvedRoots)
    {
        foreach (var root in resolvedRoots)
        {
            if (File.Exists(root) &&
                Path.GetFileName(root).Equals("Romestead.dll", StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            if (Directory.Exists(root))
            {
                var candidate = Path.Combine(root, "Romestead.dll");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static List<string> FindModLoaderMarkers(string assemblyPath)
    {
        var markers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var readerParameters = new ReaderParameters
        {
            ReadingMode = ReadingMode.Deferred,
            ReadSymbols = false
        };

        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
        foreach (var reference in assembly.MainModule.AssemblyReferences)
        {
            if (reference.Name.Contains("ModLoader", StringComparison.OrdinalIgnoreCase))
            {
                markers.Add($"assembly-ref:{reference.Name}");
            }
        }

        foreach (var type in assembly.MainModule.Types)
        {
            if (type.FullName.Contains("ModLoader", StringComparison.OrdinalIgnoreCase))
            {
                markers.Add($"type:{type.FullName}");
            }
        }

        return markers.OrderBy(marker => marker, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string ComputeFileSha256(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(stream));
    }

    private static string ComputeFileSha1(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha1 = SHA1.Create();
        return Convert.ToHexString(sha1.ComputeHash(stream));
    }

    private static bool MatchesAny(string? value, IReadOnlyCollection<string> patterns)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return patterns.Any(pattern => value.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static int ShowHelp()
    {
        Console.WriteLine(
            """
            romestead-ref scan [--root <dir-or-dll>] [--out <dir>] [--baseline <snapshot.json>] [--patch-label <label>] [--known-clean <json>] [--expected-manifest <id>] [--require-known-clean] [--assembly <pattern>] [--assembly-exact <name>] [--include-all-assemblies] [--include-compiler-generated] [--allow-modded]
              Builds a stable API snapshot plus a searchable HTML wiki.
              By default it refuses to scan Romestead.dll when it differs from Romestead.dll.modloader-backup or mod-loader markers are detected.
              Use --require-known-clean or --known-clean to verify Romestead.dll size/SHA1 against a manifest allowlist before scanning.
              If --baseline is omitted and <out>/snapshot.json already exists, it diffs against the previous run automatically.

            romestead-ref diff --old <snapshot.json> --new <snapshot.json> [--out <dir>] [--patch-label <label>]
              Compares two snapshots and writes or prints a change report.

            romestead-ref commands [--root <dir-or-dll>] [--out <dir>] [--old <commands.json>] [--known-clean <json>] [--expected-manifest <id>] [--require-known-clean] [--assembly <pattern>] [--assembly-exact <name>] [--allow-modded]
              Extracts only terminal dot commands and writes commands.html/json/md.
              If --old is omitted and <out>/commands.json already exists, it diffs against that previous command catalog.
              This command does not rewrite snapshot.json, diff.html, or the full published catalog.

            romestead-ref find <pattern> [--root <dir-or-dll>] [--assembly <pattern>] [--assembly-exact <name>]
            romestead-ref inspect <pattern> [--root <dir-or-dll>] [--assembly <pattern>] [--assembly-exact <name>]
            romestead-ref calls <pattern> [--root <dir-or-dll>] [--assembly <pattern>] [--assembly-exact <name>]
            romestead-ref xref <pattern> [--root <dir-or-dll>] [--assembly <pattern>] [--assembly-exact <name>]
            romestead-ref relate <pattern> [--root <dir-or-dll>] [--assembly <pattern>] [--assembly-exact <name>]
              Starts from an existing loader function or API name and prints related bridge methods,
              neighboring game methods, and suggested API member sketches.

            Examples:
              dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- scan
              dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- scan --patch-label "0.25.1_5 + 0.25.1_6"
              dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- scan --assembly-exact Romestead --assembly-exact Shared --out .\output\romestead-api
              dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- diff --old .\output\latest\history\snapshot-20260527-180000.json --new .\output\latest\snapshot.json
              dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- commands --root "C:\Program Files (x86)\Steam\steamapps\common\romestead" --out .\output\commands
              dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- relate RevealAll
            """);
        return 0;
    }

    private sealed record CleanDllCheckResult(bool IsClean, string Message);

    private sealed record KnownCleanRomesteadFile(
        string? Label,
        string? AppId,
        string? DepotId,
        string? ManifestId,
        string? File,
        long Size,
        string Sha1);

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
