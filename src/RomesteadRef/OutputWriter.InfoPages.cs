using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private static string BuildAboutPage(CatalogSnapshot snapshot, CatalogDiff? diff)
    {
        var builder = new StringBuilder();
        AppendDocumentStart(builder, "About - Romestead Assembly Reference", "style.css", "", "about", diff is not null);
        builder.AppendLine("<p class=\"breadcrumb\"><a href=\"index.html\">Catalog</a></p>");
        builder.AppendLine("<header class=\"site-header\"><div><h1>About This Reference</h1>");
        builder.AppendLine($"<p class=\"lede\">Generated {Encode(snapshot.Metadata.GeneratedAtUtc.ToString("u"))} from {snapshot.Metadata.AssemblyCount} scanned assemblies.</p></div>");
        builder.AppendLine("<nav class=\"header-actions\"><a class=\"button\" href=\"README.md\">README</a><a class=\"button\" href=\"site-manifest.json\">Manifest</a><a class=\"button\" href=\"snapshot.json\">Snapshot JSON</a></nav></header>");

        builder.AppendLine("<section class=\"panel docs-panel\"><h2>Purpose</h2>");
        builder.AppendLine("<p>This site is a generated DLL reference for Romestead modding. It is meant to help mod authors find classes, inspect members, copy patch targets, and understand nearby call relationships without opening a decompiler for every lookup.</p>");
        builder.AppendLine("<p class=\"notice\">This is not official game documentation. It is a generated reference built from scanned DLL metadata and should be verified against the game version you are targeting.</p>");
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"panel docs-panel\"><h2>Community Links</h2>");
        builder.AppendLine("<ul class=\"list\">");
        builder.AppendLine("<li><span>Romestead Wiki</span><a href=\"https://romestead.wiki.gg/\">romestead.wiki.gg</a></li>");
        builder.AppendLine("<li><span>Official Romestead Discord</span><a href=\"https://discord.gg/q7DP3GGrgZ\">discord.gg/q7DP3GGrgZ</a></li>");
        builder.AppendLine("</ul></section>");

        builder.AppendLine("<section class=\"panel docs-panel\"><h2>What Is Included</h2><div class=\"docs-grid\">");
        builder.AppendLine($"<article><h3>Assemblies</h3><p>{Encode(string.Join(", ", snapshot.Assemblies.Select(assembly => assembly.Name)))}</p></article>");
        builder.AppendLine($"<article><h3>Catalog Size</h3><p>{snapshot.Metadata.TypeCount:N0} types, {snapshot.Metadata.MethodCount:N0} methods, {snapshot.Metadata.PropertyCount:N0} properties, {snapshot.Metadata.FieldCount:N0} fields.</p></article>");
        builder.AppendLine("<article><h3>Exports</h3><p>Every type page has a matching JSON file. The full snapshot is available as <code>snapshot.json</code>.</p></article>");
        builder.AppendLine("</div></section>");

        if (diff is not null)
        {
            builder.AppendLine("<section class=\"panel compact-panel\"><div class=\"section-heading\"><h2>Latest Patch Diff</h2><a href=\"diff.html\">Open diff</a></div>");
            builder.AppendLine("<ul class=\"stat-list\">");
            builder.AppendLine($"<li><span>Types</span><strong>+{diff.Summary.AddedTypes} / -{diff.Summary.RemovedTypes} / ~{diff.Summary.ChangedTypes}</strong></li>");
            builder.AppendLine($"<li><span>Methods</span><strong>+{diff.Summary.AddedMethods} / -{diff.Summary.RemovedMethods} / ~{diff.Summary.ChangedMethods}</strong></li>");
            builder.AppendLine($"<li><span>Fields</span><strong>+{diff.Summary.AddedFields} / -{diff.Summary.RemovedFields} / ~{diff.Summary.ChangedFields}</strong></li>");
            builder.AppendLine("</ul></section>");
        }

        AppendDocumentEnd(builder, "About this reference", "", "README.md", "site-manifest.json", "snapshot.json");
        return builder.ToString();
    }

    private static string BuildGuidePage(CatalogSnapshot snapshot, CatalogDiff? diff)
    {
        var builder = new StringBuilder();
        AppendDocumentStart(builder, "Modder Guide - Romestead Assembly Reference", "style.css", "", "guide", diff is not null);
        builder.AppendLine("<p class=\"breadcrumb\"><a href=\"index.html\">Catalog</a></p>");
        builder.AppendLine("<header class=\"site-header\"><div><h1>Modder Guide</h1>");
        builder.AppendLine("<p class=\"lede\">How to use this generated reference when exploring game types, choosing patch targets, and checking update breakage.</p></div>");
        builder.AppendLine("<nav class=\"header-actions\"><a class=\"button\" href=\"index.html\">Search catalog</a><a class=\"button\" href=\"namespaces.html\">Namespaces</a><a class=\"button\" href=\"snapshot.json\">Snapshot JSON</a></nav></header>");

        builder.AppendLine("<section class=\"panel docs-panel\"><h2>Fast Workflow</h2>");
        builder.AppendLine("<ol class=\"numbered-list\">");
        builder.AppendLine("<li><span>Search for a gameplay word, UI label, class name, or known method. Press <kbd>/</kbd> from the home page to jump into search.</span></li>");
        builder.AppendLine("<li><span>Open the most relevant type page and compare fields, properties, methods, base types, interfaces, and references.</span></li>");
        builder.AppendLine("<li><span>Use <strong>Copy signature</strong> or <strong>Copy Harmony target</strong> on method rows when preparing a patch.</span></li>");
        builder.AppendLine("<li><span>Check <strong>Referenced calls</strong> and <strong>Called by</strong> to understand where a method sits in the surrounding code path.</span></li>");
        builder.AppendLine("<li><span>After game updates, open the diff page and review change badges before assuming old patch targets are still safe.</span></li>");
        builder.AppendLine("</ol></section>");

        builder.AppendLine("<section class=\"panel docs-panel\"><h2>Choosing Targets</h2><div class=\"guide-grid\">");
        builder.AppendLine("<article><h3>Prefer public first</h3><p>Public members are usually less volatile than private fields or compiler-generated methods. They are still not guaranteed stable.</p></article>");
        builder.AppendLine("<article><h3>Watch overloads</h3><p>Names alone are not enough. Copy the full signature when a type has multiple methods with the same name.</p></article>");
        builder.AppendLine("<article><h3>Use relationships</h3><p>Base types, interfaces, derived types, and referenced-by sections help find adjacent systems without guessing namespace names.</p></article>");
        builder.AppendLine("<article><h3>Treat internals carefully</h3><p>Private and internal members are useful for patches, but minor updates can rename, remove, or rewrite them.</p></article>");
        builder.AppendLine("</div></section>");

        builder.AppendLine("<section class=\"panel docs-panel\"><h2>What The Exports Are For</h2>");
        builder.AppendLine("<ul class=\"list\">");
        builder.AppendLine("<li><span><code>snapshot.json</code></span><span>Complete generated catalog for scripts, external search, or custom tooling.</span></li>");
        builder.AppendLine("<li><span>Type JSON files</span><span>Per-type metadata stored next to each generated type page.</span></li>");
        builder.AppendLine("<li><span><code>site-manifest.json</code></span><span>Small metadata file for publishing, indexing, or linking generated builds.</span></li>");
        builder.AppendLine("<li><span><code>diff.json</code></span><span>Machine-readable patch comparison when a baseline is available.</span></li>");
        builder.AppendLine("</ul></section>");

        builder.AppendLine("<section class=\"panel docs-panel\"><h2>Current Snapshot</h2>");
        builder.AppendLine("<ul class=\"list compact\">");
        builder.AppendLine("<li><span>Build metadata</span><code>site-manifest.json</code></li>");
        builder.AppendLine($"<li><span>Assemblies</span><span>{snapshot.Metadata.AssemblyCount:N0}</span></li>");
        builder.AppendLine($"<li><span>Types</span><span>{snapshot.Metadata.TypeCount:N0}</span></li>");
        builder.AppendLine($"<li><span>Methods</span><span>{snapshot.Metadata.MethodCount:N0}</span></li>");
        builder.AppendLine("</ul></section>");

        AppendDocumentEnd(builder, "Modder guide", "", "index.html", "about.html", "README.md", "site-manifest.json");
        return builder.ToString();
    }

    private static string RenderReadme(CatalogSnapshot snapshot, CatalogDiff? diff)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Romestead Assembly Reference");
        builder.AppendLine();
        builder.AppendLine("Generated static reference for Romestead modding. This site indexes scanned game assemblies so mod authors can find types, inspect members, copy Harmony target strings, and follow best-effort call relationships.");
        builder.AppendLine();
        builder.AppendLine("Community links:");
        builder.AppendLine();
        builder.AppendLine("- Romestead Wiki: <https://romestead.wiki.gg/>");
        builder.AppendLine("- Official Romestead Discord: <https://discord.gg/q7DP3GGrgZ>");
        builder.AppendLine();
        builder.AppendLine("## Entry Points");
        builder.AppendLine();
        builder.AppendLine("- `index.html` - main reference home and search");
        builder.AppendLine("- `guide.html` - modder-oriented usage guide");
        builder.AppendLine("- `namespaces.html` - namespace browser");
        builder.AppendLine("- `topics.html` - heuristic topic browser");
        builder.AppendLine("- `diff.html` - latest patch diff");
        builder.AppendLine("- `snapshot.json` - complete catalog snapshot");
        builder.AppendLine("- `site-manifest.json` - generated site metadata");
        builder.AppendLine();
        builder.AppendLine("## Current Snapshot");
        builder.AppendLine();
        builder.AppendLine("- Build metadata: `site-manifest.json`");
        builder.AppendLine($"- Assemblies: `{snapshot.Metadata.AssemblyCount}`");
        builder.AppendLine($"- Types: `{snapshot.Metadata.TypeCount}`");
        builder.AppendLine($"- Methods: `{snapshot.Metadata.MethodCount}`");
        builder.AppendLine($"- Properties: `{snapshot.Metadata.PropertyCount}`");
        builder.AppendLine($"- Fields: `{snapshot.Metadata.FieldCount}`");
        builder.AppendLine($"- Scanned assemblies: {string.Join(", ", snapshot.Assemblies.Select(assembly => $"`{assembly.Name}`"))}");
        if (diff is not null)
        {
            builder.AppendLine();
            builder.AppendLine("## Latest Diff");
            builder.AppendLine();
            builder.AppendLine($"- Types: `+{diff.Summary.AddedTypes} / -{diff.Summary.RemovedTypes} / ~{diff.Summary.ChangedTypes}`");
            builder.AppendLine($"- Methods: `+{diff.Summary.AddedMethods} / -{diff.Summary.RemovedMethods} / ~{diff.Summary.ChangedMethods}`");
            builder.AppendLine($"- Fields: `+{diff.Summary.AddedFields} / -{diff.Summary.RemovedFields} / ~{diff.Summary.ChangedFields}`");
        }
        builder.AppendLine();
        builder.AppendLine("## Hosting On GitHub Pages");
        builder.AppendLine();
        builder.AppendLine("This folder is ready to publish as a static site. Keep `.nojekyll` in the published root so GitHub Pages serves generated files directly without Jekyll processing.");
        builder.AppendLine();
        builder.AppendLine("Recommended layout:");
        builder.AppendLine();
        builder.AppendLine("- Publish this generated folder as the Pages root for a docs-only repository.");
        builder.AppendLine("- Or copy it under a versioned folder such as `latest/` or `builds/YYYY-MM-DD/` and link to those versions from a small repository home page.");
        builder.AppendLine();
        builder.AppendLine("## Notes For Modders");
        builder.AppendLine();
        builder.AppendLine("- Public members are usually safer references, but this catalog does not guarantee API stability.");
        builder.AppendLine("- Private and internal members are included for Harmony patch authors. Treat them as patch targets, not stable contracts.");
        builder.AppendLine("- Caller/callee relationships are best-effort and based on IL method references.");
        builder.AppendLine("- Topic pages are heuristic groupings, not curated official API categories.");
        return builder.ToString();
    }

    private static string BuildSiteManifest(CatalogSnapshot snapshot, CatalogDiff? diff)
    {
        var manifest = new
        {
            title = "Romestead Assembly Reference",
            generatedAtUtc = snapshot.Metadata.GeneratedAtUtc,
            assemblies = snapshot.Assemblies.Select(assembly => new
            {
                assembly.Name,
                assembly.Version,
                assembly.Hash,
                assembly.TypeCount,
                assembly.MethodCount,
                assembly.PropertyCount,
                assembly.FieldCount
            }).ToArray(),
            totals = new
            {
                snapshot.Metadata.AssemblyCount,
                snapshot.Metadata.TypeCount,
                snapshot.Metadata.MethodCount,
                snapshot.Metadata.PropertyCount,
                snapshot.Metadata.FieldCount
            },
            latestDiff = diff is null
                ? null
                : new
                {
                    diff.Metadata?.PatchLabel,
                    diff.Metadata?.BaselineSnapshotLabel,
                    diff.Metadata?.OldGeneratedAtUtc,
                    diff.Metadata?.NewGeneratedAtUtc,
                    diff.Summary.AddedTypes,
                    diff.Summary.RemovedTypes,
                    diff.Summary.ChangedTypes,
                    diff.Summary.AddedMethods,
                    diff.Summary.RemovedMethods,
                    diff.Summary.ChangedMethods,
                    diff.Summary.AddedFields,
                    diff.Summary.RemovedFields,
                    diff.Summary.ChangedFields
                },
            pages = new[]
            {
                "index.html",
                "guide.html",
                "about.html",
                "namespaces.html",
                "topics.html",
                "diff.html",
                "snapshot.json"
            }
        };

        return JsonSerializer.Serialize(manifest, JsonOptions);
    }
}
