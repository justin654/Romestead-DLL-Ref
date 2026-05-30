using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private static TopicGroup[] BuildTopicGroups(CatalogSnapshot snapshot)
    {
        var definitions = new[]
        {
            new TopicDefinition("Inventory & Items", ["inventory", "item", "equipment", "loot", "chest", "coin", "money", "weapon", "shield"]),
            new TopicDefinition("World & Map", ["world", "map", "chunk", "tile", "biome", "town", "dungeon", "interior", "exterior"]),
            new TopicDefinition("Combat & Bosses", ["combat", "damage", "spell", "projectile", "weapon", "boss", "enemy", "health", "kill"]),
            new TopicDefinition("UI & Input", ["ui", "window", "control", "panel", "menu", "tooltip", "button", "input", "keyboard", "mouse"]),
            new TopicDefinition("Networking", ["network", "server", "client", "message", "peer", "riptide", "litenet", "multiplayer"]),
            new TopicDefinition("Data & Content", ["data", "setup", "recipe", "icon", "asset", "localization", "definition", "resource"]),
            new TopicDefinition("Quests & Progression", ["quest", "reward", "offering", "blessing", "worship", "skill", "favour", "ordinance"]),
            new TopicDefinition("Graphics & Audio", ["graphics", "render", "sprite", "texture", "font", "sound", "audio", "fmod", "particle", "vfx"]),
        };

        var allTypes = snapshot.Assemblies
            .SelectMany(assembly => assembly.Types.Select(type => new CatalogTypeEntry(assembly, type)))
            .ToArray();

        return definitions
            .Select(definition =>
            {
                var matches = allTypes
                    .Where(entry => TopicMatches(entry.Type, definition.Tokens))
                    .OrderBy(entry => entry.Type.FullName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                return new TopicGroup(definition.Name, matches);
            })
            .ToArray();
    }

    private static bool TopicMatches(TypeCatalog type, IReadOnlyList<string> tokens)
    {
        var haystack = string.Join(
            " ",
            type.FullName,
            type.Namespace,
            type.DisplayName,
            type.BaseType,
            string.Join(" ", type.Interfaces));

        return tokens.Any(token => haystack.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildNamespaceAnchor(string assemblyName, string namespaceName) =>
        BuildAnchor($"{assemblyName}-{namespaceName}");
}
