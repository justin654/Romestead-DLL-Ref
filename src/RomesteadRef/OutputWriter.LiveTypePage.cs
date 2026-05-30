using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private static string BuildTypePage(
        CatalogSnapshot snapshot,
        AssemblyCatalog assembly,
        TypeCatalog type,
        CatalogPageContext context)
    {
        var typeLinks = context.TypeLinks;
        var typeKey = BuildCatalogKey(assembly.Name, type.Id);
        var typeChange = context.TypeChanges.GetValueOrDefault(typeKey);
        context.DerivedTypes.TryGetValue(type.FullName, out var derivedTypes);
        context.InterfaceImplementors.TryGetValue(type.FullName, out var implementors);
        context.TypeReferences.TryGetValue(type.FullName, out var referencingTypes);

        var builder = new StringBuilder();
        AppendDocumentStart(
            builder,
            $"{type.FullName} - Romestead Assembly Reference",
            "../../style.css",
            "../../",
            "types",
            context.HasDiff,
            inlineScript: $"{BuildMemberFilterScript()}{Environment.NewLine}{BuildCopyScript()}");
        builder.AppendLine($"<p class=\"breadcrumb\"><a href=\"../../index.html\">Catalog</a> / <a href=\"../../assemblies/{Encode(GetAssemblyFileName(assembly.Name))}\">{Encode(assembly.Name)}</a></p>");
        builder.AppendLine("<div class=\"title-row\">");
        builder.AppendLine($"<h1>{Encode(type.FullName)}</h1>");
        builder.AppendLine($"<button class=\"copy-button\" type=\"button\" data-copy=\"{Encode(type.FullName)}\">Copy type</button>");
        builder.AppendLine($"<button class=\"copy-button\" type=\"button\" data-copy=\"{Encode(BuildTypeMarkdown(assembly, type))}\">Copy markdown</button>");
        builder.AppendLine($"<a class=\"button\" href=\"{Encode(GetTypeJsonFileName(type))}\">Type JSON</a>");
        if (!string.IsNullOrWhiteSpace(typeChange))
        {
            builder.AppendLine($"<span class=\"change-badge change-{Encode(typeChange)}\">{Encode(typeChange)}</span>");
        }
        builder.AppendLine("</div>");
        builder.AppendLine($"<p class=\"lede\">{Encode(type.Visibility)} {Encode(type.Kind)} in <code>{Encode(type.Namespace)}</code></p>");

        builder.AppendLine("<section class=\"panel\">");
        builder.AppendLine("<div class=\"member-summary\">");
        builder.AppendLine(MetricCard("Methods", type.Methods.Count.ToString("N0")));
        builder.AppendLine(MetricCard("Properties", type.Properties.Count.ToString("N0")));
        builder.AppendLine(MetricCard("Fields", type.Fields.Count.ToString("N0")));
        builder.AppendLine(MetricCard("Events", type.Events.Count.ToString("N0")));
        builder.AppendLine("</div>");
        builder.AppendLine("<details class=\"debug-details\">");
        builder.AppendLine("<summary>Type metadata</summary>");
        builder.AppendLine("<ul class=\"list compact\">");
        builder.AppendLine($"<li><span>Assembly</span><code>{Encode(assembly.Name)}</code></li>");
        builder.AppendLine($"<li><span>Namespace</span><code>{Encode(type.Namespace)}</code></li>");
        if (!string.IsNullOrWhiteSpace(type.BaseType))
        {
            builder.AppendLine($"<li><span>Base type</span><code>{LinkSignature(type.BaseType!, typeLinks, "../../")}</code></li>");
        }
        if (type.Interfaces.Count > 0)
        {
            builder.AppendLine($"<li><span>Interfaces</span><code>{LinkSignature(string.Join(", ", type.Interfaces), typeLinks, "../../")}</code></li>");
        }
        builder.AppendLine($"<li><span>Hash</span><code>{Encode(type.Hash)}</code></li>");
        builder.AppendLine("</ul>");
        builder.AppendLine("</details>");
        builder.AppendLine("</section>");

        if (!string.IsNullOrWhiteSpace(type.BaseType) || type.Interfaces.Count > 0 || (derivedTypes?.Count ?? 0) > 0 || (implementors?.Count ?? 0) > 0 || (referencingTypes?.Count ?? 0) > 0)
        {
            builder.AppendLine("<section class=\"panel\">");
            builder.AppendLine("<h2>Relationships</h2>");
            builder.AppendLine("<div class=\"relationship-grid\">");
            if (!string.IsNullOrWhiteSpace(type.BaseType))
            {
                builder.AppendLine("<article><h3>Base type</h3>");
                builder.AppendLine($"<p><code>{LinkSignature(type.BaseType!, typeLinks, "../../")}</code></p></article>");
            }
            if (type.Interfaces.Count > 0)
            {
                builder.AppendLine("<article><h3>Interfaces</h3><ul class=\"list compact\">");
                foreach (var interfaceName in type.Interfaces)
                {
                    builder.AppendLine($"<li><code>{LinkSignature(interfaceName, typeLinks, "../../")}</code></li>");
                }
                builder.AppendLine("</ul></article>");
            }
            if ((derivedTypes?.Count ?? 0) > 0)
            {
                builder.AppendLine("<article><h3>Derived types</h3><ul class=\"list compact\">");
                foreach (var entry in derivedTypes!.Take(12))
                {
                    builder.AppendLine($"<li><a href=\"../../{Encode(context.TypeLinks[entry.Type.FullName])}\"><code>{Encode(entry.Type.FullName)}</code></a></li>");
                }
                builder.AppendLine("</ul></article>");
            }
            if ((implementors?.Count ?? 0) > 0)
            {
                builder.AppendLine("<article><h3>Implementors</h3><ul class=\"list compact\">");
                foreach (var entry in implementors!.Take(12))
                {
                    builder.AppendLine($"<li><a href=\"../../{Encode(context.TypeLinks[entry.Type.FullName])}\"><code>{Encode(entry.Type.FullName)}</code></a></li>");
                }
                builder.AppendLine("</ul></article>");
            }
            if ((referencingTypes?.Count ?? 0) > 0)
            {
                builder.AppendLine("<article><h3>Referenced by types</h3><ul class=\"list compact\">");
                foreach (var entry in referencingTypes!.Take(12))
                {
                    builder.AppendLine($"<li><a href=\"../../{Encode(context.TypeLinks[entry.Type.FullName])}\"><code>{Encode(entry.Type.FullName)}</code></a></li>");
                }
                builder.AppendLine("</ul></article>");
            }
            builder.AppendLine("</div></section>");
        }

        if (type.SourceLocations.Count > 0)
        {
            builder.AppendLine("<section class=\"panel\">");
            builder.AppendLine("<h2>Source hints</h2>");
            builder.AppendLine("<ul class=\"list\">");
            foreach (var source in type.SourceLocations)
            {
                builder.AppendLine($"<li><code>{Encode(source.DocumentPath)}</code>{(source.StartLine is null ? "" : $" : {source.StartLine}")}</li>");
            }
            builder.AppendLine("</ul>");
            builder.AppendLine("</section>");
        }

        builder.AppendLine("<section class=\"panel member-tools\">");
        builder.AppendLine("<div class=\"member-toolbar\">");
        builder.AppendLine("<label for=\"memberSearch\">Filter members</label>");
        builder.AppendLine("<input id=\"memberSearch\" type=\"search\" placeholder=\"method, field, return type, or parameter\">");
        builder.AppendLine("<label><input id=\"publicOnly\" type=\"checkbox\" checked> Public only</label>");
        builder.AppendLine("<label><input id=\"patchTargetsOnly\" type=\"checkbox\"> Patch targets</label>");
        builder.AppendLine("<select id=\"memberKind\"><option value=\"all\">All members</option><option value=\"method\">Methods</option><option value=\"property\">Properties</option><option value=\"field\">Fields</option><option value=\"event\">Events</option></select>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");

        var removedMethods = context.RemovedMethods.GetValueOrDefault(typeKey);
        var removedProperties = context.RemovedProperties.GetValueOrDefault(typeKey);
        var removedFields = context.RemovedFields.GetValueOrDefault(typeKey);
        if ((removedMethods?.Count ?? 0) > 0 || (removedProperties?.Count ?? 0) > 0 || (removedFields?.Count ?? 0) > 0)
        {
            builder.AppendLine(BuildRemovedMembersSection(
                removedMethods,
                removedProperties,
                removedFields,
                context.DiffLabel));
        }

        if (type.Fields.Count > 0)
        {
            builder.AppendLine("<section class=\"panel\">");
            builder.AppendLine("<h2>Fields</h2>");
            builder.AppendLine("<table class=\"reference-table\"><thead><tr><th>Name</th><th>Visibility</th><th>Signature</th></tr></thead><tbody>");
            foreach (var field in type.Fields)
            {
                var memberChange = context.FieldChanges.GetValueOrDefault(BuildCatalogKey(assembly.Name, type.Id, field.Id));
                builder.AppendLine($"<tr class=\"member-entry\" data-member-kind=\"field\" data-member-visibility=\"{Encode(field.Visibility)}\" data-patch-target=\"false\" data-member-text=\"{Encode($"{field.Name} {field.Signature} {field.Type}")}\" id=\"{Encode(BuildAnchor(field.Id))}\"><td>{BuildMemberTitle(field.Name, field.Signature, memberChange, BuildAnchor(field.Id))}</td><td>{Encode(field.Visibility)}</td><td><code>{LinkSignature(field.Signature, typeLinks, "../../")}</code></td></tr>");
            }
            builder.AppendLine("</tbody></table>");
            builder.AppendLine("</section>");
        }

        if (type.Properties.Count > 0)
        {
            builder.AppendLine("<section class=\"panel\">");
            builder.AppendLine("<h2>Properties</h2>");
            builder.AppendLine("<table class=\"reference-table\"><thead><tr><th>Name</th><th>Visibility</th><th>Signature</th></tr></thead><tbody>");
            foreach (var property in type.Properties)
            {
                var memberChange = context.PropertyChanges.GetValueOrDefault(BuildCatalogKey(assembly.Name, type.Id, property.Id));
                builder.AppendLine($"<tr class=\"member-entry\" data-member-kind=\"property\" data-member-visibility=\"{Encode(property.Visibility)}\" data-patch-target=\"false\" data-member-text=\"{Encode($"{property.Name} {property.Signature} {property.Type}")}\" id=\"{Encode(BuildAnchor(property.Id))}\"><td>{BuildMemberTitle(property.Name, property.Signature, memberChange, BuildAnchor(property.Id))}</td><td>{Encode(property.Visibility)}</td><td><code>{LinkSignature(property.Signature, typeLinks, "../../")}</code></td></tr>");
            }
            builder.AppendLine("</tbody></table>");
            builder.AppendLine("</section>");
        }

        if (type.Events.Count > 0)
        {
            builder.AppendLine("<section class=\"panel\">");
            builder.AppendLine("<h2>Events</h2>");
            builder.AppendLine("<table class=\"reference-table\"><thead><tr><th>Name</th><th>Visibility</th><th>Signature</th></tr></thead><tbody>");
            foreach (var eventInfo in type.Events)
            {
                builder.AppendLine($"<tr class=\"member-entry\" data-member-kind=\"event\" data-member-visibility=\"{Encode(eventInfo.Visibility)}\" data-patch-target=\"false\" data-member-text=\"{Encode($"{eventInfo.Name} {eventInfo.Signature} {eventInfo.Type}")}\" id=\"{Encode(BuildAnchor(eventInfo.Id))}\"><td>{BuildMemberTitle(eventInfo.Name, eventInfo.Signature, null, BuildAnchor(eventInfo.Id))}</td><td>{Encode(eventInfo.Visibility)}</td><td><code>{LinkSignature(eventInfo.Signature, typeLinks, "../../")}</code></td></tr>");
            }
            builder.AppendLine("</tbody></table>");
            builder.AppendLine("</section>");
        }

        if (type.Methods.Count > 0)
        {
            builder.AppendLine("<section class=\"panel\">");
            builder.AppendLine("<h2>Methods</h2>");
            foreach (var method in type.Methods)
            {
                var methodSearchText = $"{method.Name} {method.Signature} {method.ReturnType} {string.Join(" ", method.Parameters.Select(parameter => $"{parameter.Name} {parameter.Type}"))}";
                var methodChange = context.MethodChanges.GetValueOrDefault(BuildCatalogKey(assembly.Name, type.Id, method.Id));
                var methodAnchor = BuildAnchor(method.Id);
                var methodReferenceKey = BuildMethodReferenceKey(type, method);
                context.Callers.TryGetValue(methodReferenceKey, out var callers);
                builder.AppendLine($"<article class=\"member-entry method\" data-member-kind=\"method\" data-member-visibility=\"{Encode(method.Visibility)}\" data-patch-target=\"{(method.BodyHash is null ? "false" : "true")}\" data-member-text=\"{Encode(methodSearchText)}\" id=\"{Encode(methodAnchor)}\">");
                builder.AppendLine("<div class=\"method-heading\">");
                builder.AppendLine($"<h3><code>{Encode(method.Name)}</code></h3>");
                builder.AppendLine($"<a class=\"permalink\" href=\"#{Encode(methodAnchor)}\" aria-label=\"Permalink\">#</a>");
                builder.AppendLine($"<button class=\"copy-button\" type=\"button\" data-copy=\"{Encode(method.Signature)}\">Copy signature</button>");
                builder.AppendLine($"<button class=\"copy-button\" type=\"button\" data-copy=\"{Encode(BuildHarmonyTarget(type, method))}\">Copy Harmony target</button>");
                if (!string.IsNullOrWhiteSpace(methodChange))
                {
                    builder.AppendLine($"<span class=\"change-badge change-{Encode(methodChange)}\">{Encode(methodChange)}</span>");
                }
                builder.AppendLine("</div>");
                builder.AppendLine($"<p><code>{LinkSignature(method.Signature, typeLinks, "../../")}</code></p>");
                if (method.Source is not null)
                {
                    builder.AppendLine($"<p class=\"muted\"><code>{Encode(method.Source.DocumentPath)}</code>{(method.Source.StartLine is null ? "" : $" : {method.Source.StartLine}")}</p>");
                }

                builder.AppendLine("<details class=\"debug-details\"><summary>Method metadata</summary><ul class=\"list compact\">");
                builder.AppendLine($"<li><span>Visibility</span><span>{Encode(method.Visibility)}</span></li>");
                builder.AppendLine($"<li><span>Return type</span><code>{LinkSignature(method.ReturnType, typeLinks, "../../")}</code></li>");
                builder.AppendLine($"<li><span>Static</span><span>{method.IsStatic}</span></li>");
                builder.AppendLine($"<li><span>Virtual</span><span>{method.IsVirtual}</span></li>");
                builder.AppendLine($"<li><span>Hash</span><code>{Encode(method.Hash)}</code></li>");
                if (!string.IsNullOrWhiteSpace(method.BodyHash))
                {
                    builder.AppendLine($"<li><span>Body hash</span><code>{Encode(method.BodyHash!)}</code></li>");
                    builder.AppendLine($"<li><span>IL size</span><span>{method.IlSize}</span></li>");
                }
                builder.AppendLine("</ul></details>");

                if (method.CalledMethods.Count > 0)
                {
                    builder.AppendLine("<details><summary>Referenced calls</summary><ul class=\"list compact\">");
                    foreach (var call in method.CalledMethods)
                    {
                        builder.AppendLine($"<li><code>{LinkMethodReference(call, context.MethodReferenceLinks, "../../")}</code></li>");
                    }
                    builder.AppendLine("</ul></details>");
                }

                if ((callers?.Count ?? 0) > 0)
                {
                    builder.AppendLine("<details><summary>Called by</summary><ul class=\"list compact\">");
                    foreach (var caller in callers!.Take(32))
                    {
                        builder.AppendLine($"<li><a href=\"../../{Encode(caller.Link)}\"><code>{Encode($"{caller.Type.FullName}::{caller.Method.Name}")}</code></a></li>");
                    }
                    builder.AppendLine("</ul></details>");
                }

                if (method.StringLiterals.Count > 0)
                {
                    builder.AppendLine("<details><summary>String literals</summary><ul class=\"list compact\">");
                    foreach (var text in method.StringLiterals)
                    {
                        builder.AppendLine($"<li><code>{Encode(text)}</code></li>");
                    }
                    builder.AppendLine("</ul></details>");
                }

                builder.AppendLine("</article>");
            }

            builder.AppendLine("</section>");
        }

        AppendDocumentEnd(builder, $"Catalog generated {snapshot.Metadata.GeneratedAtUtc:u}", "../../", "about.html", "README.md", "site-manifest.json");
        return builder.ToString();
    }
}
