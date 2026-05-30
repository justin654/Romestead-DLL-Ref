using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public static void PrepareOutputDirectory(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        foreach (var file in new[]
                 {
                     "index.html",
                     "api-opportunities.html",
                     "api-opportunities.json",
                     "api-opportunities.md",
                     "style.css",
                     "search-index.js",
                     "snapshot.json",
                     "diff.json",
                     "diff.md",
                     "diff.html",
                     "namespaces.html",
                     "topics.html",
                     "guide.html",
                     "about.html",
                     "README.md",
                     "site-manifest.json",
                     ".nojekyll"
                 })
        {
            var path = Path.Combine(outputDirectory, file);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        DeleteDirectoryIfExists(Path.Combine(outputDirectory, "assemblies"));
        DeleteDirectoryIfExists(Path.Combine(outputDirectory, "types"));
    }

    public static void WriteCatalogSite(
        string outputDirectory,
        CatalogBuildResult buildResult,
        CatalogSnapshot? baselineSnapshot,
        CatalogDiff? diff,
        ApiOpportunityReport apiOpportunityReport)
    {
        File.WriteAllText(Path.Combine(outputDirectory, "style.css"), BuildStyleSheet());
        var removedTypePages = BuildRemovedTypePages(baselineSnapshot, diff);
        File.WriteAllText(
            Path.Combine(outputDirectory, "search-index.js"),
            BuildSearchIndexScript(buildResult.Snapshot, removedTypePages, GetDiffDisplayLabel(diff)));
        File.WriteAllText(Path.Combine(outputDirectory, "api-opportunities.json"), JsonSerializer.Serialize(apiOpportunityReport, JsonOptions));
        File.WriteAllText(Path.Combine(outputDirectory, "api-opportunities.md"), RenderApiOpportunityMarkdown(apiOpportunityReport));

        var assembliesDirectory = Path.Combine(outputDirectory, "assemblies");
        var typesDirectory = Path.Combine(outputDirectory, "types");
        Directory.CreateDirectory(assembliesDirectory);
        Directory.CreateDirectory(typesDirectory);
        var typeLinks = BuildTypeLinkIndex(buildResult.Snapshot, removedTypePages);
        var context = BuildPageContext(buildResult.Snapshot, baselineSnapshot, diff, typeLinks);

        foreach (var assembly in buildResult.Snapshot.Assemblies)
        {
            var assemblyFileName = GetAssemblyFileName(assembly.Name);
            File.WriteAllText(
                Path.Combine(assembliesDirectory, assemblyFileName),
                BuildAssemblyPage(
                    buildResult.Snapshot,
                    assembly,
                    diff is not null,
                    removedTypePages.Where(entry => string.Equals(entry.Assembly.Name, assembly.Name, StringComparison.Ordinal)).ToArray(),
                    GetDiffDisplayLabel(diff)));

            var assemblyTypeDirectory = Path.Combine(typesDirectory, GetDirectorySafeName(assembly.Name));
            Directory.CreateDirectory(assemblyTypeDirectory);

            foreach (var type in assembly.Types)
            {
                File.WriteAllText(
                    Path.Combine(assemblyTypeDirectory, GetTypeFileName(type)),
                    BuildTypePage(buildResult.Snapshot, assembly, type, context));
                File.WriteAllText(
                    Path.Combine(assemblyTypeDirectory, GetTypeJsonFileName(type)),
                    JsonSerializer.Serialize(type, JsonOptions));
            }
        }

        foreach (var removedTypePage in removedTypePages)
        {
            var assemblyTypeDirectory = Path.Combine(typesDirectory, GetDirectorySafeName(removedTypePage.Assembly.Name));
            Directory.CreateDirectory(assemblyTypeDirectory);
            File.WriteAllText(
                Path.Combine(assemblyTypeDirectory, GetTypeFileName(removedTypePage.Type)),
                BuildRemovedTypePage(removedTypePage, typeLinks, GetDiffDisplayLabel(diff)));
            File.WriteAllText(
                Path.Combine(assemblyTypeDirectory, GetTypeJsonFileName(removedTypePage.Type)),
                JsonSerializer.Serialize(removedTypePage.Type, JsonOptions));
        }

        File.WriteAllText(Path.Combine(outputDirectory, "index.html"), BuildIndexPage(buildResult, diff, apiOpportunityReport));
        File.WriteAllText(Path.Combine(outputDirectory, "namespaces.html"), BuildNamespacesPage(buildResult.Snapshot));
        File.WriteAllText(Path.Combine(outputDirectory, "topics.html"), BuildTopicsPage(buildResult.Snapshot));
        File.WriteAllText(Path.Combine(outputDirectory, "guide.html"), BuildGuidePage(buildResult.Snapshot, diff));
        File.WriteAllText(Path.Combine(outputDirectory, "api-opportunities.html"), BuildApiOpportunityPage(apiOpportunityReport, diff is not null));
        File.WriteAllText(Path.Combine(outputDirectory, "about.html"), BuildAboutPage(buildResult.Snapshot, diff));
        File.WriteAllText(Path.Combine(outputDirectory, "README.md"), RenderReadme(buildResult.Snapshot, diff));
        File.WriteAllText(Path.Combine(outputDirectory, "site-manifest.json"), BuildSiteManifest(buildResult.Snapshot, diff));
        File.WriteAllText(Path.Combine(outputDirectory, ".nojekyll"), "");
        if (diff is not null)
        {
            WriteDiffSite(outputDirectory, diff);
        }
    }

    public static void WriteDiffSite(string outputDirectory, CatalogDiff diff)
    {
        File.WriteAllText(Path.Combine(outputDirectory, "style.css"), BuildStyleSheet());
        File.WriteAllText(Path.Combine(outputDirectory, "diff.html"), BuildDiffPage(diff));
    }
}
