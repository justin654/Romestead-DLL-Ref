using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    public static string WriteCommandsBundle(
        string outputDirectory,
        TerminalCommandCatalog catalog,
        TerminalCommandDiff? diff)
    {
        Directory.CreateDirectory(outputDirectory);

        var stylePath = Path.Combine(outputDirectory, "style.css");
        if (!File.Exists(stylePath))
        {
            File.WriteAllText(stylePath, BuildStyleSheet());
        }

        File.WriteAllText(
            Path.Combine(outputDirectory, "commands.html"),
            BuildCommandsPage(catalog, diff, includeDiff: File.Exists(Path.Combine(outputDirectory, "diff.html"))));
        File.WriteAllText(Path.Combine(outputDirectory, "commands.json"), JsonSerializer.Serialize(catalog, JsonOptions));
        File.WriteAllText(Path.Combine(outputDirectory, "commands.md"), RenderCommandsMarkdown(catalog, diff));

        var diffPath = Path.Combine(outputDirectory, "commands-diff.json");
        if (diff is null)
        {
            if (File.Exists(diffPath))
            {
                File.Delete(diffPath);
            }
        }
        else
        {
            File.WriteAllText(diffPath, JsonSerializer.Serialize(diff, JsonOptions));
        }

        var historyDirectory = Path.Combine(outputDirectory, "history");
        Directory.CreateDirectory(historyDirectory);
        var historyPath = Path.Combine(
            historyDirectory,
            $"commands-{catalog.Metadata.GeneratedAtUtc:yyyyMMdd-HHmmss}.json");
        File.WriteAllText(historyPath, JsonSerializer.Serialize(catalog, JsonOptions));
        UpdateCommandsInSiteManifest(outputDirectory, catalog, diff);

        return historyPath;
    }

    private static void UpdateCommandsInSiteManifest(
        string outputDirectory,
        TerminalCommandCatalog catalog,
        TerminalCommandDiff? diff)
    {
        var manifestPath = Path.Combine(outputDirectory, "site-manifest.json");
        if (!File.Exists(manifestPath))
        {
            return;
        }

        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath)) as JsonObject;
        if (manifest is null)
        {
            return;
        }

        manifest["terminalCommands"] = new JsonObject
        {
            ["generatedAtUtc"] = catalog.Metadata.GeneratedAtUtc.ToString("O"),
            ["commandCount"] = catalog.Metadata.CommandCount,
            ["moddingUsefulCount"] = catalog.Commands.Count(command => command.IsModdingUseful),
            ["registryMethod"] = catalog.Metadata.RegistryMethod,
            ["registryStorage"] = catalog.Metadata.RegistryStorage,
            ["autocompletePath"] = catalog.Metadata.AutocompletePath
        };

        manifest["terminalCommandDiff"] = diff is null
            ? null
            : new JsonObject
            {
                ["added"] = diff.Summary.Added,
                ["removed"] = diff.Summary.Removed,
                ["changed"] = diff.Summary.Changed
            };

        if (manifest["pages"] is not JsonArray pages)
        {
            pages = [];
            manifest["pages"] = pages;
        }

        foreach (var page in new[] { "commands.html", "commands.json", "commands.md" })
        {
            if (!pages.Any(node => string.Equals(node?.GetValue<string>(), page, StringComparison.Ordinal)))
            {
                pages.Add(page);
            }
        }

        File.WriteAllText(manifestPath, manifest.ToJsonString(JsonOptions));
    }

    public static string RenderCommandsMarkdown(TerminalCommandCatalog catalog, TerminalCommandDiff? diff)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Romestead Terminal Dot Commands");
        builder.AppendLine();
        builder.AppendLine($"Generated: `{catalog.Metadata.GeneratedAtUtc:u}`");
        builder.AppendLine($"Command count: `{catalog.Metadata.CommandCount}`");
        builder.AppendLine();
        builder.AppendLine("## How Commands Work");
        builder.AppendLine();
        builder.AppendLine($"- Registry: `{catalog.Metadata.RegistryMethod}`");
        builder.AppendLine($"- Storage: `{catalog.Metadata.RegistryStorage}`");
        builder.AppendLine($"- Dispatch: `{catalog.Metadata.DispatchPath}`");
        builder.AppendLine($"- Autocomplete: `{catalog.Metadata.AutocompletePath}`");

        if (diff is not null)
        {
            builder.AppendLine();
            builder.AppendLine("## Latest Command Diff");
            builder.AppendLine();
            builder.AppendLine($"- Added: `{diff.Summary.Added}`");
            builder.AppendLine($"- Removed: `{diff.Summary.Removed}`");
            builder.AppendLine($"- Changed: `{diff.Summary.Changed}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Full Command List");
        builder.AppendLine();
        builder.AppendLine("| Command | Usage | Category | Summary | Autocomplete | Handler |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var command in catalog.Commands.OrderBy(command => command.Name, StringComparer.Ordinal))
        {
            builder.AppendLine($"| `{command.DotName}` | `{command.Usage}` | {command.Category} | {command.Summary} | {command.AutocompleteSource ?? command.SuggestionKind} | `{command.HandlerMethod}` |");
        }

        return builder.ToString();
    }

    private static string BuildCommandsPage(TerminalCommandCatalog catalog, TerminalCommandDiff? diff, bool includeDiff)
    {
        var builder = new StringBuilder();
        AppendDocumentStart(
            builder,
            "Terminal Commands - Romestead Assembly Reference",
            "style.css",
            "",
            "commands",
            includeDiff,
            inlineScript: BuildCommandFilterScript());

        builder.AppendLine("<p class=\"breadcrumb\"><a href=\"index.html\">Catalog</a></p>");
        builder.AppendLine("<header class=\"site-header\"><div><h1>Terminal Dot Commands</h1>");
        builder.AppendLine("<p class=\"lede\">Shareable reference for the in-game terminal commands registered by Romestead.</p>");
        builder.AppendLine($"<p class=\"notice\">Generated {Encode(catalog.Metadata.GeneratedAtUtc.ToString("u"))}. Commands are discovered from <code>{Encode(catalog.Metadata.RegistryMethod)}</code>; behavior summaries are community notes inferred from handler code.</p>");
        builder.AppendLine("</div><nav class=\"header-actions\">");
        builder.AppendLine("<a class=\"button primary-button\" href=\"#all-commands\">All commands</a>");
        builder.AppendLine("<a class=\"button\" href=\"commands.json\">Commands JSON</a>");
        builder.AppendLine("<a class=\"button\" href=\"commands.md\">Markdown</a>");
        builder.AppendLine("</nav></header>");

        builder.AppendLine("<section class=\"panel compact-panel\"><ul class=\"stat-list\">");
        builder.AppendLine($"<li><span>Commands</span><strong>{catalog.Metadata.CommandCount:N0}</strong></li>");
        builder.AppendLine($"<li><span>Autocomplete-backed</span><strong>{catalog.Commands.Count(command => command.SuggestionKind != "none"):N0}</strong></li>");
        builder.AppendLine("</ul></section>");

        builder.AppendLine("<section class=\"panel docs-panel\"><div class=\"section-heading\"><h2>How The Terminal Works</h2><a href=\"types/Romestead/Candide_Terminal_GameTerminal.html\">Open GameTerminal type</a></div>");
        builder.AppendLine("<div class=\"docs-grid\">");
        builder.AppendLine($"<article><h3>Default mode</h3><p>Non-dot input is sent to Lua. Dot input strips the leading <code>.</code> and runs through <code>{Encode(catalog.Metadata.DispatchPath)}</code>.</p></article>");
        builder.AppendLine($"<article><h3>Registry</h3><p>Commands are registered in <code>{Encode(catalog.Metadata.RegistryMethod)}</code> and stored in <code>{Encode(catalog.Metadata.RegistryStorage)}</code>.</p></article>");
        builder.AppendLine($"<article><h3>Autocomplete</h3><p>Tab completion uses <code>{Encode(catalog.Metadata.AutocompletePath)}</code>. Argument completion appears only when the command provides a suggestion delegate.</p></article>");
        builder.AppendLine("</div></section>");

        builder.AppendLine("<section class=\"panel docs-panel\"><h2>Debug Mode Notes</h2>");
        builder.AppendLine("<ul class=\"list compact\">");
        builder.AppendLine("<li><span><code>.debuggeneral</code> toggles <code>Candide.Globals.Debug</code>.</span></li>");
        builder.AppendLine("<li><span>Several debug-only UI paths are gated behind <code>Globals.Debug</code>; availability depends on the current game screen and state.</span></li>");
        builder.AppendLine("<li><span><code>PlayerInventoryWindow</code> creates the debug item browser when <code>Globals.Debug</code>, cheats, and inventory extensions are enabled.</span></li>");
        builder.AppendLine("</ul></section>");

        if (diff is not null)
        {
            AppendCommandDiffSection(builder, diff);
        }

        builder.AppendLine("<section id=\"all-commands\" class=\"panel\">");
        builder.AppendLine("<div class=\"section-heading\"><h2>All Commands</h2><span>Filter by command, category, usage, summary, or handler.</span></div>");
        builder.AppendLine("<div class=\"member-toolbar command-toolbar\">");
        builder.AppendLine("<label for=\"commandSearch\">Filter commands</label>");
        builder.AppendLine("<input id=\"commandSearch\" type=\"search\" placeholder=\"poi, spawn, debug, camera, construction\">");
        builder.AppendLine("<select id=\"commandCategory\"><option value=\"all\">All categories</option>");
        foreach (var category in catalog.Commands.Select(command => command.Category).Distinct(StringComparer.Ordinal).OrderBy(category => category, StringComparer.Ordinal))
        {
            builder.AppendLine($"<option value=\"{Encode(category)}\">{Encode(category)}</option>");
        }
        builder.AppendLine("</select>");
        builder.AppendLine("</div>");
        builder.AppendLine("<table class=\"reference-table command-table\"><thead><tr><th>Command</th><th>Usage</th><th>Category</th><th>Summary</th><th>Autocomplete</th><th>Handler</th></tr></thead><tbody>");
        foreach (var command in catalog.Commands.OrderBy(command => command.Name, StringComparer.Ordinal))
        {
            var searchText = string.Join(" ", command.Name, command.Usage, command.Category, command.Summary, command.AutocompleteSource, command.HandlerMethod);
            builder.AppendLine($"<tr id=\"{Encode(BuildAnchor(command.Name))}\" class=\"command-entry\" data-command-text=\"{Encode(searchText)}\" data-command-category=\"{Encode(command.Category)}\">");
            builder.AppendLine($"<td><code>{Encode(command.DotName)}</code></td>");
            builder.AppendLine($"<td><code>{Encode(command.Usage)}</code></td>");
            builder.AppendLine($"<td>{Encode(command.Category)}</td>");
            builder.AppendLine($"<td>{Encode(command.Summary)}</td>");
            builder.AppendLine($"<td>{Encode(command.AutocompleteSource ?? command.SuggestionKind)}</td>");
            builder.AppendLine($"<td><code>{Encode(TrimHandlerName(command.HandlerMethod))}</code></td>");
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody></table></section>");

        AppendDocumentEnd(builder, "Terminal command reference", "", "index.html", "commands.json", "commands.md", "site-manifest.json");
        return builder.ToString();
    }

    private static void AppendCommandDiffSection(StringBuilder builder, TerminalCommandDiff diff)
    {
        builder.AppendLine("<section class=\"panel compact-panel\">");
        builder.AppendLine("<div class=\"section-heading\"><h2>Latest Command Diff</h2><span>Compared with previous commands.json</span></div>");
        builder.AppendLine("<ul class=\"stat-list\">");
        builder.AppendLine($"<li><span>Added</span><strong>+{diff.Summary.Added}</strong></li>");
        builder.AppendLine($"<li><span>Removed</span><strong>-{diff.Summary.Removed}</strong></li>");
        builder.AppendLine($"<li><span>Changed</span><strong>~{diff.Summary.Changed}</strong></li>");
        builder.AppendLine("</ul>");
        if (diff.Summary.Added + diff.Summary.Removed + diff.Summary.Changed == 0)
        {
            builder.AppendLine("<p class=\"notice\">No command registry changes detected.</p>");
        }
        else
        {
            builder.AppendLine("<table class=\"reference-table diff-table\"><thead><tr><th>Change</th><th>Command</th><th>Old</th><th>New</th></tr></thead><tbody>");
            foreach (var command in diff.Added)
            {
                builder.AppendLine($"<tr><td><span class=\"change-badge change-added\">added</span></td><td><code>{Encode(command.DotName)}</code></td><td></td><td>{Encode(command.Summary)}</td></tr>");
            }
            foreach (var command in diff.Removed)
            {
                builder.AppendLine($"<tr><td><span class=\"change-badge change-removed\">removed</span></td><td><code>{Encode(command.DotName)}</code></td><td>{Encode(command.Summary)}</td><td></td></tr>");
            }
            foreach (var change in diff.Changed)
            {
                builder.AppendLine($"<tr><td><span class=\"change-badge change-changed\">changed</span></td><td><code>.{Encode(change.Name)}</code></td><td>{Encode(change.OldCommand.HandlerBodyHash ?? "<none>")}</td><td>{Encode(change.NewCommand.HandlerBodyHash ?? "<none>")}</td></tr>");
            }
            builder.AppendLine("</tbody></table>");
        }
        builder.AppendLine("</section>");
    }

    private static string BuildCommandFilterScript() =>
        """
        document.addEventListener("DOMContentLoaded", () => {
          const search = document.getElementById("commandSearch");
          const category = document.getElementById("commandCategory");
          const entries = Array.from(document.querySelectorAll(".command-entry"));
          const apply = () => {
            const query = (search?.value || "").trim().toLowerCase();
            const selectedCategory = category?.value || "all";
            for (const entry of entries) {
              const text = (entry.dataset.commandText || "").toLowerCase();
              const matchesQuery = !query || text.includes(query);
              const matchesCategory = selectedCategory === "all" || entry.dataset.commandCategory === selectedCategory;
              entry.hidden = !(matchesQuery && matchesCategory);
            }
          };
          search?.addEventListener("input", apply);
          category?.addEventListener("change", apply);
          apply();
        });
        """;

    private static string TrimHandlerName(string handler)
    {
        const string prefix = "System.Object ";
        return handler.StartsWith(prefix, StringComparison.Ordinal) ? handler[prefix.Length..] : handler;
    }
}
