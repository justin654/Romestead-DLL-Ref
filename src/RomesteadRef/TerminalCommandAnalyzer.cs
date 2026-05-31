using System.Security.Cryptography;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RomesteadRef;

internal sealed class TerminalCommandAnalyzer
{
    private const string RomesteadAssemblyName = "Romestead";
    private const string EngineTypeName = "Candide.CandideEngine";
    private const string TerminalTypeName = "Candide.Terminal.GameTerminal";
    private const string RegistryMethodName = "AddTerminalCommands";

    public TerminalCommandCatalog Analyze(IReadOnlyList<string> includedAssemblyPaths, DateTime generatedAtUtc)
    {
        var romesteadPath = includedAssemblyPaths.FirstOrDefault(path =>
            string.Equals(Path.GetFileNameWithoutExtension(path), RomesteadAssemblyName, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(romesteadPath) || !File.Exists(romesteadPath))
        {
            return EmptyCatalog(generatedAtUtc);
        }

        var resolver = new DefaultAssemblyResolver();
        foreach (var directory in includedAssemblyPaths
                     .Select(Path.GetDirectoryName)
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            resolver.AddSearchDirectory(directory!);
        }

        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = resolver,
            ReadingMode = ReadingMode.Deferred
        };

        using var assembly = AssemblyDefinition.ReadAssembly(romesteadPath, readerParameters);
        var engineType = FlattenTypes(assembly.MainModule.Types)
            .FirstOrDefault(type => string.Equals(type.FullName, EngineTypeName, StringComparison.Ordinal));
        var registryMethod = engineType?.Methods
            .FirstOrDefault(method => string.Equals(method.Name, RegistryMethodName, StringComparison.Ordinal));

        var commands = registryMethod is null
            ? []
            : ExtractCommands(registryMethod);

        return new TerminalCommandCatalog(
            new TerminalCommandMetadata(
                GeneratedAtUtc: generatedAtUtc,
                SourceAssemblyPath: romesteadPath,
                SourceAssemblySha256: ComputeFileSha256(romesteadPath),
                RegistryType: EngineTypeName,
                RegistryMethod: $"{EngineTypeName}.{RegistryMethodName}()",
                RegistryStorage: $"{TerminalTypeName}.Commands: Dictionary<string, TerminalCommand>",
                DispatchPath: $"{TerminalTypeName}.DoCommand(string) after stripping the leading dot",
                AutocompletePath: $"{TerminalTypeName}.SuggestCommand(string), using Commands.Keys plus per-command suggestion delegates",
                RegistryBodyHash: registryMethod is null ? null : ComputeMethodBodyHash(registryMethod),
                CommandCount: commands.Count),
            commands);
    }

    private static TerminalCommandCatalog EmptyCatalog(DateTime generatedAtUtc) =>
        new(
            new TerminalCommandMetadata(
                GeneratedAtUtc: generatedAtUtc,
                SourceAssemblyPath: null,
                SourceAssemblySha256: null,
                RegistryType: EngineTypeName,
                RegistryMethod: $"{EngineTypeName}.{RegistryMethodName}()",
                RegistryStorage: $"{TerminalTypeName}.Commands: Dictionary<string, TerminalCommand>",
                DispatchPath: $"{TerminalTypeName}.DoCommand(string) after stripping the leading dot",
                AutocompletePath: $"{TerminalTypeName}.SuggestCommand(string), using Commands.Keys plus per-command suggestion delegates",
                RegistryBodyHash: null,
                CommandCount: 0),
            []);

    private static IReadOnlyList<TerminalCommandEntry> ExtractCommands(MethodDefinition registryMethod)
    {
        if (!registryMethod.HasBody)
        {
            return [];
        }

        var entries = new List<TerminalCommandEntry>();
        var instructions = registryMethod.Body.Instructions;
        for (var index = 0; index < instructions.Count; index++)
        {
            var instruction = instructions[index];
            if (!IsAddCommandCall(instruction.Operand))
            {
                continue;
            }

            var commandNameIndex = FindPreviousCommandNameInstruction(instructions, index);
            if (commandNameIndex < 0)
            {
                continue;
            }

            var name = instructions[commandNameIndex].Operand as string;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var delegates = FindDelegateTargets(instructions, commandNameIndex, index);
            var handler = delegates.FirstOrDefault(IsActionHandler) ?? delegates.FirstOrDefault();
            var suggestions = delegates.Where(method => !ReferenceEquals(method, handler)).ToArray();
            var definition = CommandDefinitions.TryGetValue(name, out var known)
                ? known
                : CommandDefinition.Unknown(name);

            var suggestionKind = GetSuggestionKind(suggestions);
            var suggestionMethod = suggestions.Length == 0
                ? null
                : string.Join("; ", suggestions.Select(FormatMethodReference));
            var suggestionBodyHash = suggestions.Length == 0
                ? null
                : ComputeHash(string.Join("\n", suggestions.Select(method => ComputeResolvedMethodBodyHash(method) ?? "")));
            var handlerBodyHash = handler is null ? null : ComputeResolvedMethodBodyHash(handler);
            var changeHash = ComputeHash(string.Join(
                "|",
                name,
                handlerBodyHash ?? "",
                suggestionKind,
                suggestionBodyHash ?? "",
                definition.AutocompleteSource ?? ""));

            entries.Add(new TerminalCommandEntry(
                Order: entries.Count + 1,
                Name: name,
                DotName: $".{name}",
                Usage: definition.Usage,
                Summary: definition.Summary,
                Category: definition.Category,
                IsModdingUseful: definition.ModdingUseful,
                HandlerMethod: handler is null ? "<unknown>" : FormatMethodReference(handler),
                HandlerBodyHash: handlerBodyHash,
                SuggestionKind: suggestionKind,
                SuggestionMethod: suggestionMethod,
                SuggestionBodyHash: suggestionBodyHash,
                AutocompleteSource: definition.AutocompleteSource,
                SourceOffset: $"IL_{instruction.Offset:X4}",
                ChangeHash: changeHash));
        }

        return entries;
    }

    private static bool IsAddCommandCall(object? operand) =>
        operand is MethodReference method &&
        string.Equals(method.Name, "AddCommand", StringComparison.Ordinal) &&
        string.Equals(method.DeclaringType.FullName, TerminalTypeName, StringComparison.Ordinal);

    private static int FindPreviousCommandNameInstruction(IList<Instruction> instructions, int beforeIndex)
    {
        for (var index = beforeIndex - 1; index >= 0; index--)
        {
            if (instructions[index].OpCode.Code == Code.Ldstr)
            {
                return index;
            }
        }

        return -1;
    }

    private static MethodReference[] FindDelegateTargets(IList<Instruction> instructions, int startIndex, int endIndex)
    {
        var targets = new List<MethodReference>();
        for (var index = startIndex + 1; index < endIndex; index++)
        {
            if ((instructions[index].OpCode.Code == Code.Ldftn ||
                 instructions[index].OpCode.Code == Code.Ldvirtftn) &&
                instructions[index].Operand is MethodReference method)
            {
                targets.Add(method);
            }
        }

        return [.. targets];
    }

    private static bool IsActionHandler(MethodReference method) =>
        string.Equals(method.ReturnType.FullName, "System.Object", StringComparison.Ordinal);

    private static string GetSuggestionKind(IReadOnlyList<MethodReference> suggestions)
    {
        if (suggestions.Count == 0)
        {
            return "none";
        }

        return suggestions.Any(method => method.Parameters.Count == 1 &&
                                         string.Equals(method.Parameters[0].ParameterType.FullName, "System.Int32", StringComparison.Ordinal))
            ? "argument-indexed"
            : "single-list";
    }

    private static IEnumerable<TypeDefinition> FlattenTypes(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            yield return type;
            foreach (var nested in FlattenTypes(type.NestedTypes))
            {
                yield return nested;
            }
        }
    }

    private static string FormatMethodReference(MethodReference method) =>
        $"{method.ReturnType.FullName} {method.DeclaringType.FullName}::{method.Name}({string.Join(", ", method.Parameters.Select(parameter => parameter.ParameterType.FullName))})";

    private static string? ComputeResolvedMethodBodyHash(MethodReference method)
    {
        try
        {
            var resolved = method.Resolve();
            return resolved is null ? null : ComputeMethodBodyHash(resolved);
        }
        catch
        {
            return null;
        }
    }

    private static string? ComputeMethodBodyHash(MethodDefinition method)
    {
        if (!method.HasBody)
        {
            return null;
        }

        var builder = new StringBuilder();
        foreach (var instruction in method.Body.Instructions)
        {
            builder.Append(instruction.OpCode.Code).Append('|');
            builder.Append(NormalizeOperand(instruction.Operand)).AppendLine();
        }

        return ComputeHash(builder.ToString());
    }

    private static string NormalizeOperand(object? operand) =>
        operand switch
        {
            null => "",
            MethodReference methodReference => FormatMethodReference(methodReference),
            FieldReference fieldReference => $"{fieldReference.DeclaringType.FullName}::{fieldReference.Name}",
            TypeReference typeReference => typeReference.FullName,
            ParameterDefinition parameterDefinition => $"arg:{parameterDefinition.Index}:{parameterDefinition.Name}",
            VariableDefinition variableDefinition => $"var:{variableDefinition.Index}:{variableDefinition.VariableType.FullName}",
            Instruction => "target",
            Instruction[] instructions => $"targets:{instructions.Length}",
            _ => operand.ToString() ?? ""
        };

    private static string ComputeFileSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string ComputeHash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed record CommandDefinition(
        string Usage,
        string Summary,
        string Category,
        bool ModdingUseful,
        string? AutocompleteSource)
    {
        public static CommandDefinition Unknown(string name) =>
            new($".{name}", "Registered terminal command. Inspect the handler for behavior details.", "Other", false, null);
    }

    private static readonly Dictionary<string, CommandDefinition> CommandDefinitions = new(StringComparer.Ordinal)
    {
        ["load"] = new(".load <script-path>", "Imports a script file and runs it as Lua.", "Lua / scripting", false, null),
        ["item"] = new(".item <item-id> [count-or-aura]", "Adds an item cheat when connected and cheats are enabled.", "Items", true, "ItemDataBase.DataMap keys; ItemAuraDataBase.AuraDataMap keys"),
        ["spawn"] = new(".spawn <entity-or-doodad-name-or-guid>", "Spawns one entity or doodad at the player, or requests the server to spawn it.", "Entities / spawning", true, "DoodadDatabaseManager.Names keys"),
        ["line_spawn"] = new(".line_spawn <entity-or-doodad-name-or-guid>", "Spawns or requests a 40-entity line.", "Entities / spawning", true, "DoodadDatabaseManager.Names keys"),
        ["mass_spawn"] = new(".mass_spawn <entity-or-doodad-name-or-guid>", "Spawns or requests a 25 by 40 entity grid.", "Entities / spawning", true, "DoodadDatabaseManager.Names keys"),
        ["dynamic-spawn"] = new(".dynamic-spawn <entity-or-doodad-name-or-guid> [instant-aggro]", "Requests dynamic entity spawning, optionally with instant aggro.", "Entities / spawning", true, "DoodadDatabaseManager.Names keys"),
        ["spawn_all_creatures"] = new(".spawn_all_creatures", "Spawns every creature GUID near the player.", "Entities / spawning", true, null),
        ["construct"] = new(".construct <construction-id>", "Requests construction placement at the player.", "Constructions / placeables", true, "ConstructionDataBase.DataMap keys"),
        ["speed"] = new(".speed <int>", "Sets player acceleration scale.", "Player / movement", false, null),
        ["reloadsprites"] = new(".reloadsprites", "Reloads sprite sheets.", "Reload / content", true, null),
        ["unlearn_all_favours"] = new(".unlearn_all_favours", "Removes all learned favours.", "Progression", false, null),
        ["reloadparticles"] = new(".reloadparticles", "Clears and reloads particle data.", "Reload / content", true, null),
        ["reloaddata"] = new(".reloaddata", "Reloads exterior auto-tile rules.", "Reload / content", true, null),
        ["noclip"] = new(".noclip", "Toggles player collision.", "Player / movement", true, null),
        ["float"] = new(".float", "Toggles player gravity.", "Player / movement", true, null),
        ["tp"] = new(".tp <tile-x> <tile-y>", "Teleports to tile coordinates; the handler multiplies by 16 and jumps the camera.", "Player / movement", true, null),
        ["debug3d"] = new(".debug3d", "Toggles deferred renderer debug mode.", "Debug / overlays", false, null),
        ["debugtiles"] = new(".debugtiles", "Toggles tile debug rendering.", "Debug / overlays", true, null),
        ["debugcrops"] = new(".debugcrops", "Toggles crop debug rendering.", "Debug / overlays", false, null),
        ["debuggeneral"] = new(".debuggeneral", "Toggles Globals.Debug, enabling broad debug UI features.", "Debug / overlays", true, null),
        ["debugspeed"] = new(".debugspeed", "Toggles movement-speed debug rendering.", "Debug / overlays", false, null),
        ["debugchunks"] = new(".debugchunks", "Toggles exterior world chunk debug rendering.", "Debug / overlays", true, null),
        ["tae"] = new(".tae <index>", "Enables a time analyzer.", "Diagnostics", false, null),
        ["tad"] = new(".tad <index>", "Disables a time analyzer.", "Diagnostics", false, null),
        ["taa"] = new(".taa <index>", "Analyzes a time analyzer.", "Diagnostics", false, null),
        ["tac"] = new(".tac <index>", "Clears logs for a time analyzer.", "Diagnostics", false, null),
        ["mode"] = new(".mode <int>", "Sets the engine mode manager by numeric value.", "Debug / mode", false, null),
        ["ui"] = new(".ui", "Toggles game UI visibility.", "Debug / overlays", true, null),
        ["imgui"] = new(".imgui", "Toggles ImGui.", "Debug / overlays", false, null),
        ["grid"] = new(".grid", "Toggles tile grid rendering.", "Debug / overlays", true, null),
        ["beat"] = new(".beat", "Starts the beat engine.", "Debug / misc", false, null),
        ["playerstate"] = new(".playerstate", "Prints the current player state machine state.", "Diagnostics", false, null),
        ["renderscale"] = new(".renderscale <int>", "Sets render scale.", "Display / camera", false, null),
        ["uiscale"] = new(".uiscale <int>", "Sets UI scale.", "Display / camera", false, null),
        ["fontscale"] = new(".fontscale <float>", "Sets terminal font scale.", "Display / camera", false, null),
        ["hitboxes"] = new(".hitboxes", "Toggles hitbox rendering.", "Debug / overlays", true, null),
        ["firstperson"] = new(".firstperson", "Toggles first-person/deferred-renderer debug mode.", "Display / camera", false, null),
        ["jump"] = new(".jump", "Applies upward player velocity.", "Player / movement", false, null),
        ["chat"] = new(".chat <message...>", "Sends chat text.", "Networking / chat", false, null),
        ["name"] = new(".name [name...]", "Sets player name; no argument reports the current name.", "Player / profile", false, null),
        ["gametimescale"] = new(".gametimescale [float]", "Sets global game time scale.", "Time / cheats", false, null),
        ["timescale"] = new(".timescale [int]", "Sets day/night cycle time scale.", "Time / cheats", false, null),
        ["cheat_crop_growth_scale"] = new(".cheat_crop_growth_scale [int]", "Sets crop growth time scale.", "Time / cheats", false, null),
        ["cheat_job_speed_scale"] = new(".cheat_job_speed_scale [int]", "Sets job tick speed scale.", "Time / cheats", false, null),
        ["cheat_job_progress_scale"] = new(".cheat_job_progress_scale [int]", "Sets job progress scale.", "Time / cheats", false, null),
        ["cheat_spawnrate_scale"] = new(".cheat_spawnrate_scale [int]", "Sets dynamic entity spawn-rate scale.", "Entities / spawning", true, null),
        ["cheat_disable_construction_materials"] = new(".cheat_disable_construction_materials", "Toggles construction material requirements.", "Constructions / placeables", true, null),
        ["detach"] = new(".detach", "Requests detach for the held entity.", "Player / movement", false, null),
        ["cheat_spawn_wave"] = new(".cheat_spawn_wave", "Requests an enemy wave spawn.", "Entities / spawning", false, null),
        ["nextday"] = new(".nextday", "Requests a server time skip to the next day.", "Time / cheats", false, null),
        ["hour"] = new(".hour [double]", "Requests server time set to a day-hour value.", "Time / cheats", false, null),
        ["resettime"] = new(".resettime", "Requests time reset to day 1 at 00:00.", "Time / cheats", false, null),
        ["instantactions"] = new(".instantactions", "Toggles instant actions when cheats are enabled.", "Time / cheats", false, null),
        ["health"] = new(".health [float]", "Sets player health; default is max health.", "Player / cheats", false, null),
        ["networkstats"] = new(".networkstats", "Enables network stats or prints current network stats.", "Networking / diagnostics", false, null),
        ["zoom"] = new(".zoom [float]", "Sets camera and zoom scale; default is 1.", "Display / camera", true, null),
        ["freecam"] = new(".freecam", "Toggles free camera mode.", "Display / camera", true, null),
        ["questcheat"] = new(".questcheat <quest-id>", "Requests world quest completion.", "Progression", false, "QuestsDataBase.DataMap keys"),
        ["test"] = new(".test", "Prints collision-offset entity diagnostics.", "Diagnostics", false, null),
        ["framestepper"] = new(".framestepper", "Toggles the frame stepper.", "Diagnostics", false, null),
        ["framestepper_stepframes"] = new(".framestepper_stepframes [int]", "Runs N frames with the frame stepper.", "Diagnostics", false, null),
        ["framestepper_stepcount"] = new(".framestepper_stepcount [int]", "Sets custom frame-step skip count.", "Diagnostics", false, null),
        ["toggle_spawn_system"] = new(".toggle_spawn_system", "Toggles the spawn system disabled flag.", "Entities / spawning", true, null),
        ["weather"] = new(".weather [weather-id]", "Requests a weather cheat at the player position.", "World / map", true, "ClientWeatherDatabase.Data keys"),
        ["marcopolo"] = new(".marcopolo", "Copies server world tiles/heights locally and discovers dungeons/bosses while local hosting.", "World / map", true, null),
        ["load_in_world_file"] = new(".load_in_world_file <map-name>", "Loads a world map file into the exterior world at the player while local hosting.", "World / map", true, "ServerWorldDataManager.WorldData keys"),
        ["poi"] = new(".poi <poi-id>", "Spawns a registered POI near the player.", "World / map", true, "PoiDataBase.Data keys"),
        ["enter"] = new(".enter [building-guid]", "Enters a specified building or the exterior under the player.", "World / map", true, "ServerGameState.Buildings keys"),
        ["exit"] = new(".exit [building-guid]", "Exits a building or dungeon, or exits a specified building.", "World / map", true, null),
        ["boss"] = new(".boss <boss-id>", "Requests boss spawn at the player.", "Entities / spawning", false, "BossDataBase.DataMap keys"),
        ["challenge_dungeon"] = new(".challenge_dungeon <dungeon-id>", "Requests challenge dungeon spawn.", "World / map", true, "ChallengeDungeonDataBase.DataMap keys"),
        ["dungeon"] = new(".dungeon <room-id>", "Requests handcrafted dungeon-room spawn.", "World / map", true, "DungeonRoomDataBase.DataMap handcrafted room keys"),
        ["aura"] = new(".aura <aura-id>", "Applies an aura to the player.", "Player / cheats", false, "AuraDataBase.AuraDataMap keys"),
        ["lagsmoothmode"] = new(".lagsmoothmode [int]", "Sets lag smoothing mode; default is 3.", "Networking / diagnostics", false, null),
        ["save_game"] = new(".save_game", "Queues server game save while local hosting.", "Saves", true, null),
        ["save_character"] = new(".save_character", "Saves the current character.", "Saves", false, null),
        ["save_worldmap"] = new(".save_worldmap", "Saves the world map.", "Saves", true, null),
        ["seed"] = new(".seed", "Copies and prints the world seed.", "World / map", true, null),
        ["die"] = new(".die", "Requests player death mode 0; arguments are rejected with 'Use .health instead'.", "Player / cheats", false, null),
        ["cinematic"] = new(".cinematic", "Enters cinematic camera mode.", "Display / camera", true, null),
        ["connection_address"] = new(".connection_address", "Copies and prints connection address or Steam relay ID.", "Networking / diagnostics", false, null),
        ["citizen_add_generic"] = new(".citizen_add_generic", "Adds one generic citizen.", "Entities / spawning", false, null),
        ["citizen_kill_all"] = new(".citizen_kill_all", "Kills all citizens.", "Entities / spawning", false, null),
        ["rewards_unlock_all"] = new(".rewards_unlock_all", "Unlocks all rewards.", "Progression", false, null),
        ["rewards_unlock_one"] = new(".rewards_unlock_one <reward-id>", "Unlocks one reward.", "Progression", false, "RewardDataBase.RewardsDataMap keys"),
        ["raid_new"] = new(".raid_new <raid-id>", "Spawns a raid targeting the town under the player.", "Raids", false, "RaidDataBase.DataMap keys"),
        ["raid_arrive"] = new(".raid_arrive", "Forces the current town raid to arrive.", "Raids", false, null),
        ["raid_beat"] = new(".raid_beat", "Marks the current town raid beaten.", "Raids", false, null),
        ["raid_fail"] = new(".raid_fail", "Marks the current town raid failed.", "Raids", false, null),
        ["buildings_construct_all"] = new(".buildings_construct_all", "Spawns every base building and upgrade chain in rows near the player.", "Constructions / placeables", true, null),
        ["all_pois"] = new(".all_pois [prefix]", "Spawns all POIs matching an optional prefix while local hosting, including flipped variants.", "World / map", true, null),
        ["furniture_add_all"] = new(".furniture_add_all [count]", "Adds every furniture entry by count when cheats are enabled.", "Constructions / placeables", true, null)
    };
}
