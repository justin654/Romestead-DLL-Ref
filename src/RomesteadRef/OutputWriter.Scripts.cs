using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private static string BuildMemberFilterScript() =>
        """
        document.addEventListener("DOMContentLoaded", () => {
          const search = document.getElementById("memberSearch");
          const publicOnly = document.getElementById("publicOnly");
          const patchTargetsOnly = document.getElementById("patchTargetsOnly");
          const kind = document.getElementById("memberKind");
          const entries = Array.from(document.querySelectorAll(".member-entry"));
          const apply = () => {
            const query = (search?.value || "").trim().toLowerCase();
            const selectedKind = kind?.value || "all";
            const onlyPublic = publicOnly?.checked ?? false;
            const onlyPatchTargets = patchTargetsOnly?.checked ?? false;
            for (const entry of entries) {
              const text = (entry.dataset.memberText || "").toLowerCase();
              const visibility = entry.dataset.memberVisibility || "";
              const entryKind = entry.dataset.memberKind || "";
              const visible = (!query || text.includes(query)) &&
                (selectedKind === "all" || selectedKind === entryKind) &&
                (!onlyPublic || visibility === "public") &&
                (!onlyPatchTargets || entry.dataset.patchTarget === "true");
              entry.hidden = !visible;
            }
          };
          search?.addEventListener("input", apply);
          publicOnly?.addEventListener("change", apply);
          patchTargetsOnly?.addEventListener("change", apply);
          kind?.addEventListener("change", apply);
          apply();
        });
        """;

    private static string BuildCopyScript() =>
        """
        document.addEventListener("click", async (event) => {
          const button = event.target.closest("[data-copy]");
          if (!button) {
            return;
          }

          try {
            await navigator.clipboard.writeText(button.dataset.copy || "");
            const originalText = button.textContent;
            button.textContent = "Copied";
            button.classList.add("copied");
            setTimeout(() => {
              button.textContent = originalText;
              button.classList.remove("copied");
            }, 1200);
          } catch {
            button.textContent = "Copy failed";
          }
        });
        """;

    private static string BuildJsonDownloadPromptScript() =>
        """
        document.addEventListener("click", event => {
          const link = event.target.closest("a[href$='.json']");
          if (!link || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
            return;
          }

          event.preventDefault();
          const fileName = link.getAttribute("href").split("/").pop() || "export.json";
          const shouldDownload = window.confirm(`${fileName} is a JSON export and may be too large to display in the browser. Download it instead?`);
          if (!shouldDownload) {
            return;
          }

          const download = document.createElement("a");
          download.href = link.href;
          download.download = fileName;
          document.body.appendChild(download);
          download.click();
          download.remove();
        });
        """;

    private static string BuildSearchIndexScript(
        CatalogSnapshot snapshot,
        IReadOnlyList<RemovedTypePageEntry> removedTypePages,
        string? diffLabel)
    {
        var searchEntries = new List<object>();
        foreach (var assembly in snapshot.Assemblies)
        {
            searchEntries.Add(new
            {
                kind = "assembly",
                title = assembly.Name,
                subtitle = $"{assembly.TypeCount} types | {assembly.MethodCount} methods",
                link = $"assemblies/{GetAssemblyFileName(assembly.Name)}",
                search = $"{assembly.Name} {string.Join(" ", assembly.References)}"
            });

            foreach (var ns in assembly.Namespaces)
            {
                searchEntries.Add(new
                {
                    kind = "namespace",
                    title = ns.Name,
                    subtitle = $"{assembly.Name} | {ns.TypeCount} types | {ns.MethodCount} methods",
                    link = $"namespaces.html#{BuildNamespaceAnchor(assembly.Name, ns.Name)}",
                    search = $"{assembly.Name} {ns.Name}"
                });
            }

            foreach (var type in assembly.Types)
            {
                var typeLink = $"types/{GetDirectorySafeName(assembly.Name)}/{GetTypeFileName(type)}";
                searchEntries.Add(new
                {
                    kind = "type",
                    title = type.FullName,
                    subtitle = $"{assembly.Name} | {type.Kind}",
                    link = typeLink,
                    search = $"{assembly.Name} {type.FullName} {type.BaseType} {string.Join(" ", type.Interfaces)}",
                    visibility = type.Visibility
                });

                foreach (var method in type.Methods)
                {
                    searchEntries.Add(new
                    {
                        kind = "method",
                        title = method.Signature,
                        subtitle = $"{assembly.Name} | {type.FullName}",
                        link = $"{typeLink}#{BuildAnchor(method.Id)}",
                        search = $"{assembly.Name} {type.FullName} {method.Signature} {method.Name}",
                        visibility = method.Visibility
                    });
                }

                foreach (var property in type.Properties)
                {
                    searchEntries.Add(new
                    {
                        kind = "property",
                        title = property.Signature,
                        subtitle = $"{assembly.Name} | {type.FullName}",
                        link = $"{typeLink}#{BuildAnchor(property.Id)}",
                        search = $"{assembly.Name} {type.FullName} {property.Signature}",
                        visibility = property.Visibility
                    });
                }

                foreach (var field in type.Fields)
                {
                    searchEntries.Add(new
                    {
                        kind = "field",
                        title = field.Signature,
                        subtitle = $"{assembly.Name} | {type.FullName}",
                        link = $"{typeLink}#{BuildAnchor(field.Id)}",
                        search = $"{assembly.Name} {type.FullName} {field.Signature}",
                        visibility = field.Visibility
                    });
                }
            }
        }

        foreach (var removedTypePage in removedTypePages)
        {
            searchEntries.Add(new
            {
                kind = "type",
                title = removedTypePage.Type.FullName,
                subtitle = $"{removedTypePage.Assembly.Name} | removed in {diffLabel ?? "latest compared patch"}",
                link = removedTypePage.Link,
                search = $"{removedTypePage.Assembly.Name} {removedTypePage.Type.FullName} removed {diffLabel}",
                visibility = removedTypePage.Type.Visibility
            });
        }

        return $"window.ROMESTEAD_REF_SEARCH_INDEX = {JsonSerializer.Serialize(searchEntries, JsonOptions)};";
    }
}
