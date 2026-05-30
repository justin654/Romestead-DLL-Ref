using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private static string BuildApiOpportunityPage(ApiOpportunityReport report, bool includeDiff)
    {
        var builder = new StringBuilder();
        AppendDocumentStart(builder, "API Opportunities - Romestead Assembly Reference", "style.css", "", "api", includeDiff);
        builder.AppendLine("<p class=\"breadcrumb\"><a href=\"index.html\">Catalog</a></p>");
        builder.AppendLine("<h1>API Opportunities</h1>");
        builder.AppendLine("<p class=\"lede\">Generated from the current mod loader abstractions and the live game entrypoints they already touch.</p>");

        builder.AppendLine("<section class=\"panel\">");
        builder.AppendLine("<h2>How to use this</h2>");
        builder.AppendLine("<ul class=\"list compact\">");
        builder.AppendLine("<li>Each section starts from an existing loader interface or registry you already expose.</li>");
        builder.AppendLine("<li>Bridge methods show where your current loader code is already reaching into game internals.</li>");
        builder.AppendLine("<li>Candidate methods are adjacent game methods on those touched types that look like likely next seams to wrap.</li>");
        builder.AppendLine("<li>Suggested API members are sketches only; use them to keep naming aligned with the loader surface you already have.</li>");
        builder.AppendLine("</ul>");
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"panel\">");
        builder.AppendLine("<h2>Surface Navigator</h2>");
        builder.AppendLine("<div class=\"surface-grid\">");
        foreach (var surface in report.Surfaces)
        {
            var topTypeNames = surface.ExternalTypes
                .Where(type => type.Candidates.Count > 0)
                .Take(2)
                .Select(type => type.DisplayName)
                .ToArray();

            builder.AppendLine($"<a class=\"surface-card\" href=\"#{Encode(surface.DomainId)}\">");
            builder.AppendLine($"<strong>{Encode(surface.Title)}</strong>");
            builder.AppendLine($"<span><code>{Encode(surface.InterfaceDisplayName)}</code></span>");
            builder.AppendLine($"<small>{surface.CurrentApiMembers.Count} api members &middot; {surface.BridgeMethods.Count} bridge methods &middot; {surface.ExternalTypes.Count(type => type.Candidates.Count > 0)} candidate types</small>");
            if (topTypeNames.Length > 0)
            {
                builder.AppendLine($"<small>Start with: {Encode(string.Join(", ", topTypeNames))}</small>");
            }
            builder.AppendLine("</a>");
        }
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");

        foreach (var surface in report.Surfaces)
        {
            var topExternalTypes = surface.ExternalTypes
                .Where(type => type.Candidates.Count > 0)
                .Take(3)
                .ToArray();

            builder.AppendLine($"<section class=\"panel\" id=\"{Encode(surface.DomainId)}\">");
            builder.AppendLine($"<h2>{Encode(surface.Title)}</h2>");
            builder.AppendLine($"<p class=\"lede\"><code>{Encode(surface.InterfaceDisplayName)}</code></p>");
            builder.AppendLine("<div class=\"surface-overview\">");
            builder.AppendLine("<article class=\"overview-card\">");
            builder.AppendLine("<h3>Current surface</h3>");
            if (surface.CurrentApiMembers.Count > 0)
            {
                builder.AppendLine("<ul class=\"list compact\">");
                foreach (var member in surface.CurrentApiMembers.Take(8))
                {
                    builder.AppendLine($"<li><code>{Encode(member.Signature)}</code></li>");
                }
                builder.AppendLine("</ul>");
            }
            if (surface.Implementations.Count > 0)
            {
                builder.AppendLine("<p class=\"muted\">Implementation entrypoints</p>");
                builder.AppendLine("<ul class=\"list compact\">");
                foreach (var implementation in surface.Implementations.Take(4))
                {
                    builder.AppendLine($"<li><code>{Encode(implementation.DisplayName)}</code></li>");
                }
                builder.AppendLine("</ul>");
            }
            builder.AppendLine("</article>");

            builder.AppendLine("<article class=\"overview-card\">");
            builder.AppendLine("<h3>Best next seams</h3>");
            if (topExternalTypes.Length > 0)
            {
                builder.AppendLine("<ul class=\"list compact\">");
                foreach (var externalType in topExternalTypes)
                {
                    var firstSuggestion = externalType.Candidates
                        .SelectMany(candidate => candidate.SuggestedApiMembers)
                        .FirstOrDefault();
                    builder.AppendLine("<li>");
                    builder.Append($"<span><code>{Encode(externalType.DisplayName)}</code>");
                    if (!string.IsNullOrWhiteSpace(firstSuggestion))
                    {
                        builder.Append($"<br><small>{Encode(firstSuggestion)}</small>");
                    }
                    builder.AppendLine("</span>");
                    builder.AppendLine($"<span>{externalType.Candidates.Count} candidate methods</span></li>");
                }
                builder.AppendLine("</ul>");
            }
            else
            {
                builder.AppendLine("<p class=\"muted\">No strong adjacent seams were ranked for this surface yet.</p>");
            }
            builder.AppendLine("</article>");

            builder.AppendLine("<article class=\"overview-card\">");
            builder.AppendLine("<h3>Bridge footprint</h3>");
            builder.AppendLine("<ul class=\"list compact\">");
            builder.AppendLine($"<li><span>Bridge methods</span><span>{surface.BridgeMethods.Count}</span></li>");
            builder.AppendLine($"<li><span>Candidate game types</span><span>{surface.ExternalTypes.Count(type => type.Candidates.Count > 0)}</span></li>");
            builder.AppendLine($"<li><span>Keywords</span><span>{Encode(string.Join(", ", surface.Keywords))}</span></li>");
            builder.AppendLine("</ul>");
            builder.AppendLine("</article>");
            builder.AppendLine("</div>");

            if (topExternalTypes.Length > 0)
            {
                builder.AppendLine("<h3>Recommended expansion targets</h3>");
                builder.AppendLine("<div class=\"opportunity-grid\">");
                foreach (var externalType in topExternalTypes)
                {
                    builder.AppendLine("<article class=\"opportunity-card\">");
                    builder.AppendLine($"<h4><code>{Encode(externalType.DisplayName)}</code></h4>");
                    builder.AppendLine($"<p class=\"muted\">{Encode(externalType.AssemblyName)} &middot; {externalType.Candidates.Count} likely adjacent methods</p>");
                    if (externalType.AlreadyTouchedMethods.Count > 0)
                    {
                        builder.AppendLine("<p class=\"muted\">Already touched</p>");
                        builder.AppendLine("<ul class=\"list compact\">");
                        foreach (var method in externalType.AlreadyTouchedMethods.Take(3))
                        {
                            builder.AppendLine($"<li><code>{Encode(method)}</code></li>");
                        }
                        builder.AppendLine("</ul>");
                    }

                    foreach (var candidate in externalType.Candidates.Take(3))
                    {
                        builder.AppendLine("<article class=\"candidate\">");
                        builder.AppendLine($"<p><code>{Encode(candidate.GameMethodSignature)}</code></p>");
                        if (candidate.SuggestedApiMembers.Count > 0)
                        {
                            builder.AppendLine("<p class=\"muted\">Sketches to expose</p><ul class=\"list compact\">");
                            foreach (var suggestion in candidate.SuggestedApiMembers.Take(2))
                            {
                                builder.AppendLine($"<li><code>{Encode(suggestion)}</code></li>");
                            }
                            builder.AppendLine("</ul>");
                        }
                        if (candidate.RelatedApiMembers.Count > 0)
                        {
                            builder.AppendLine("<p class=\"muted\">Related current API</p><ul class=\"list compact\">");
                            foreach (var related in candidate.RelatedApiMembers.Take(2))
                            {
                                builder.AppendLine($"<li><code>{Encode(related)}</code></li>");
                            }
                            builder.AppendLine("</ul>");
                        }
                        builder.AppendLine("</article>");
                    }
                    builder.AppendLine("</article>");
                }
                builder.AppendLine("</div>");
            }

            if (surface.BridgeMethods.Count > 0)
            {
                builder.AppendLine("<details class=\"section-details\"><summary>Bridge methods and touched game calls</summary>");
                foreach (var bridge in surface.BridgeMethods.Take(12))
                {
                    builder.AppendLine("<article class=\"method\">");
                    builder.AppendLine($"<h4><code>{Encode(bridge.TypeDisplayName)}</code></h4>");
                    builder.AppendLine($"<p><code>{Encode(bridge.MethodSignature)}</code></p>");
                    if (bridge.Source is not null)
                    {
                        builder.AppendLine($"<p class=\"muted\">{Encode(bridge.Source.DocumentPath)}{(bridge.Source.StartLine is null ? "" : $" : {bridge.Source.StartLine}")}</p>");
                    }
                    if (bridge.ExternalCalls.Count > 0)
                    {
                        builder.AppendLine("<details><summary>Game calls already touched</summary><ul class=\"list compact\">");
                        foreach (var call in bridge.ExternalCalls.Take(12))
                        {
                            builder.AppendLine($"<li><code>{Encode(call)}</code></li>");
                        }
                        builder.AppendLine("</ul></details>");
                    }
                    builder.AppendLine("</article>");
                }
                builder.AppendLine("</details>");
            }

            if (surface.ExternalTypes.Any(type => type.Candidates.Count > 0))
            {
                builder.AppendLine("<details class=\"section-details\"><summary>Full candidate inventory</summary>");
                foreach (var externalType in surface.ExternalTypes.Where(type => type.Candidates.Count > 0))
                {
                    builder.AppendLine("<article class=\"method\">");
                    builder.AppendLine($"<h4><code>{Encode(externalType.TypeId)}</code></h4>");
                    builder.AppendLine($"<p class=\"muted\">Assembly: {Encode(externalType.AssemblyName)}</p>");
                    if (externalType.AlreadyTouchedMethods.Count > 0)
                    {
                        builder.AppendLine("<details><summary>Methods already touched by the loader</summary><ul class=\"list compact\">");
                        foreach (var method in externalType.AlreadyTouchedMethods.Take(10))
                        {
                            builder.AppendLine($"<li><code>{Encode(method)}</code></li>");
                        }
                        builder.AppendLine("</ul></details>");
                    }
                    builder.AppendLine("<details><summary>Nearby candidate methods to wrap next</summary><div class=\"candidate-list\">");
                    foreach (var candidate in externalType.Candidates)
                    {
                        builder.AppendLine("<article class=\"candidate\">");
                        builder.AppendLine($"<p><code>{Encode(candidate.GameMethodSignature)}</code></p>");
                        if (candidate.RelatedApiMembers.Count > 0)
                        {
                            builder.AppendLine("<p class=\"muted\">Related current API members:</p><ul class=\"list compact\">");
                            foreach (var related in candidate.RelatedApiMembers)
                            {
                                builder.AppendLine($"<li><code>{Encode(related)}</code></li>");
                            }
                            builder.AppendLine("</ul>");
                        }
                        if (candidate.RelatedBridgeMethods.Count > 0)
                        {
                            builder.AppendLine("<p class=\"muted\">Related loader bridge methods:</p><ul class=\"list compact\">");
                            foreach (var related in candidate.RelatedBridgeMethods)
                            {
                                builder.AppendLine($"<li><code>{Encode(related)}</code></li>");
                            }
                            builder.AppendLine("</ul>");
                        }
                        if (candidate.SuggestedApiMembers.Count > 0)
                        {
                            builder.AppendLine("<p class=\"muted\">Suggested API member sketches:</p><ul class=\"list compact\">");
                            foreach (var suggestion in candidate.SuggestedApiMembers)
                            {
                                builder.AppendLine($"<li><code>{Encode(suggestion)}</code></li>");
                            }
                            builder.AppendLine("</ul>");
                        }
                        builder.AppendLine("</article>");
                    }
                    builder.AppendLine("</div></details>");
                    builder.AppendLine("</article>");
                }
                builder.AppendLine("</details>");
            }

            builder.AppendLine("</section>");
        }

        AppendDocumentEnd(builder, "API opportunity report", "", "index.html", "about.html", "snapshot.json");
        return builder.ToString();
    }
}
