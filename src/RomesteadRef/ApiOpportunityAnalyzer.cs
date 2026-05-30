using System.Text.RegularExpressions;

namespace RomesteadRef;

internal sealed class ApiOpportunityAnalyzer
{
    private static readonly string[] LoaderNamespacePrefixes =
    [
        "Romestead.ModLoader",
        "Romestead.StartupHook",
        "Romestead.ModLoader.ClientCore"
    ];

    private static readonly string[] InterestingMethodNameHints =
    [
        "Add", "Create", "Load", "Save", "Set", "Get", "Update", "Remove", "Register",
        "Open", "Close", "Reveal", "Spawn", "Build", "Craft", "Equip", "Drop", "Use",
        "Start", "Stop", "Change", "Unlock", "Apply", "Queue", "Map", "Inventory",
        "Recipe", "Skill", "Class", "Stat", "Quest", "Dialogue", "World", "Scene"
    ];

    private static readonly HashSet<string> GenericTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "api", "registry", "mod", "mods", "loader", "hook", "client", "core",
        "state", "service", "helper", "manager", "provider", "system", "public",
        "private", "internal", "protected", "static", "virtual", "bool", "void",
        "int", "float", "double", "string", "object", "data", "pending", "ready",
        "register", "try", "get", "set", "all", "many", "items", "recipes", "skills"
    };

    public ApiOpportunityReport Analyze(CatalogSnapshot snapshot)
    {
        var allTypes = snapshot.Assemblies
            .SelectMany(assembly => assembly.Types.Select(type => (assembly, type)))
            .ToArray();

        var loaderTypes = allTypes
            .Where(entry => LoaderNamespacePrefixes.Any(prefix =>
                entry.type.FullName.StartsWith(prefix, StringComparison.Ordinal)))
            .ToArray();

        var apiInterfaces = loaderTypes
            .Where(entry =>
                entry.type.Namespace == "Romestead.ModLoader" &&
                entry.type.Kind == "interface" &&
                (entry.type.Name.StartsWith("I", StringComparison.Ordinal) &&
                 (entry.type.Name.EndsWith("Api", StringComparison.Ordinal) ||
                  entry.type.Name.EndsWith("Registry", StringComparison.Ordinal) ||
                  entry.type.Name == "IModLifecycle" ||
                  entry.type.Name == "IContentRegistry")))
            .OrderBy(entry => entry.type.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var allLoaderMethods = loaderTypes
            .SelectMany(entry => entry.type.Methods.Select(method => new LoaderMethodIndex(entry.assembly, entry.type, method)))
            .Where(index => index.Source is not null)
            .Where(index => IsInterestingLoaderMethod(index.MethodSignature))
            .ToArray();

        var loaderMethodsByCallReference = allLoaderMethods
            .Where(index => !string.IsNullOrWhiteSpace(index.CallReference))
            .GroupBy(index => index.CallReference, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var bridgeMethods = allLoaderMethods
            .Where(index => index.ExternalCalls.Count > 0)
            .ToArray();

        var typeById = allTypes.ToDictionary(
            entry => $"{entry.assembly.Name}::{entry.type.Id}",
            StringComparer.Ordinal);

        var surfaces = new List<ApiSurfaceReport>();
        foreach (var api in apiInterfaces)
        {
            var keywords = BuildKeywords(api.type.Name);
            var currentApiMembers = BuildCurrentApiMembers(api.type);
            var implementations = loaderTypes
                .Where(entry => entry.type.Interfaces.Contains(api.type.FullName, StringComparer.Ordinal))
                .Select(entry => new ApiImplementationReport(entry.assembly.Name, entry.type.Id, entry.type.DisplayName))
                .OrderBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var implementationTypeIds = implementations
                .Select(entry => entry.TypeId)
                .ToHashSet(StringComparer.Ordinal);

            var anchoredMethods = ExpandAnchoredMethods(
                allLoaderMethods,
                loaderMethodsByCallReference,
                implementationTypeIds,
                currentApiMembers,
                keywords);

            var anchoredExternalTypeIds = anchoredMethods
                .SelectMany(method => method.ExternalCalls)
                .Select(ParseExternalMethodReference)
                .Where(reference => reference is not null)
                .Cast<ExternalMethodReference>()
                .Select(reference => reference.TypeId)
                .ToHashSet(StringComparer.Ordinal);

            var scoredBridgeMethods = bridgeMethods
                .Select(method => (method, score: ScoreMethodForSurface(
                    method,
                    keywords,
                    currentApiMembers,
                    implementations,
                    anchoredExternalTypeIds)))
                .Where(entry => entry.score >= GetSurfaceBridgeThreshold(keywords, anchoredMethods))
                .OrderByDescending(entry => entry.score)
                .ThenBy(entry => entry.method.TypeDisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.method.MethodSignature, StringComparer.OrdinalIgnoreCase)
                .Select(entry => entry.method);

            var assignedBridgeMethods = anchoredMethods
                .Concat(scoredBridgeMethods)
                .DistinctBy(entry => $"{entry.AssemblyName}::{entry.TypeId}::{entry.MethodId}")
                .Take(18)
                .ToArray();

            var externalTypeGroups = assignedBridgeMethods
                .SelectMany(method => method.ExternalCalls)
                .Select(ParseExternalMethodReference)
                .Where(reference => reference is not null)
                .Cast<ExternalMethodReference>()
                .GroupBy(reference => $"{reference.AssemblyName}::{reference.TypeId}", StringComparer.Ordinal)
                .ToArray();

            var externalTypes = new List<ExternalTypeOpportunity>();
            foreach (var group in externalTypeGroups)
            {
                if (!typeById.TryGetValue(group.Key, out var target))
                {
                    continue;
                }

                var alreadyTouched = group
                    .Select(reference => reference.DisplayName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var candidateMethods = target.type.Methods
                    .Where(method => IsInterestingCandidateMethod(method))
                    .Where(method => !alreadyTouched.Contains(method.Signature, StringComparer.Ordinal))
                    .Select(method =>
                    {
                        var candidate = BuildCandidateOpportunity(method, currentApiMembers, assignedBridgeMethods, keywords);
                        return new
                        {
                            Candidate = candidate,
                            Score = candidate?.Score ?? 0
                        };
                    })
                    .Where(entry => entry.Candidate is not null)
                    .OrderByDescending(entry => entry.Score)
                    .ThenBy(entry => entry.Candidate!.Opportunity.GameMethodSignature, StringComparer.OrdinalIgnoreCase)
                    .Select(entry => entry.Candidate!.Opportunity)
                    .DistinctBy(entry => entry.GameMethodSignature)
                    .Take(10)
                    .ToArray();

                if (candidateMethods.Length == 0 && !anchoredExternalTypeIds.Contains(target.type.Id))
                {
                    continue;
                }

                externalTypes.Add(new ExternalTypeOpportunity(
                    target.assembly.Name,
                    target.type.Id,
                    target.type.DisplayName,
                    alreadyTouched,
                    candidateMethods));
            }

            externalTypes.Sort((left, right) =>
            {
                var countCompare = right.Candidates.Count.CompareTo(left.Candidates.Count);
                return countCompare != 0
                    ? countCompare
                    : StringComparer.OrdinalIgnoreCase.Compare(left.DisplayName, right.DisplayName);
            });

            surfaces.Add(new ApiSurfaceReport(
                DomainId: NormalizeSlug(api.type.Name),
                Title: BuildTitle(api.type.Name),
                InterfaceTypeId: api.type.Id,
                InterfaceDisplayName: api.type.DisplayName,
                Keywords: keywords,
                CurrentApiMembers: currentApiMembers,
                Implementations: implementations,
                BridgeMethods: assignedBridgeMethods
                    .Select(method => new LoaderBridgeMethodReport(
                        method.AssemblyName,
                        method.TypeId,
                        method.TypeDisplayName,
                        method.MethodId,
                        method.MethodSignature,
                        method.Source,
                        method.ExternalCalls))
                    .ToArray(),
                ExternalTypes: externalTypes.Take(12).ToArray()));
        }

        return new ApiOpportunityReport(
            GeneratedAtUtc: snapshot.Metadata.GeneratedAtUtc,
            Surfaces: surfaces);
    }

    private static IReadOnlyList<ApiMemberReport> BuildCurrentApiMembers(TypeCatalog apiType)
    {
        var methods = apiType.Methods
            .Select(method => new ApiMemberReport("method", method.Name, method.Signature));
        var properties = apiType.Properties
            .Select(property => new ApiMemberReport("property", property.Name, property.Signature));

        return methods
            .Concat(properties)
            .OrderBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(member => member.Signature, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<LoaderMethodIndex> ExpandAnchoredMethods(
        IReadOnlyList<LoaderMethodIndex> allLoaderMethods,
        IReadOnlyDictionary<string, LoaderMethodIndex> loaderMethodsByCallReference,
        IReadOnlySet<string> implementationTypeIds,
        IReadOnlyList<ApiMemberReport> currentApiMembers,
        IReadOnlyList<string> keywords)
    {
        var seeds = allLoaderMethods
            .Where(method => implementationTypeIds.Contains(method.TypeId))
            .Where(method => IsImplementationAnchor(method, currentApiMembers, keywords))
            .ToArray();

        if (seeds.Length == 0)
        {
            seeds = allLoaderMethods
                .Where(method => implementationTypeIds.Contains(method.TypeId))
                .Where(method => method.ExternalCalls.Count > 0)
                .Take(4)
                .ToArray();
        }

        if (seeds.Length == 0)
        {
            return [];
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<LoaderMethodIndex>(seeds);
        var expanded = new List<LoaderMethodIndex>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var visitKey = $"{current.TypeId}::{current.MethodId}";
            if (!visited.Add(visitKey))
            {
                continue;
            }

            expanded.Add(current);

            foreach (var call in current.LoaderCalls)
            {
                if (loaderMethodsByCallReference.TryGetValue(call, out var callee))
                {
                    queue.Enqueue(callee);
                }
            }
        }

        return expanded
            .OrderBy(method => method.TypeDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(method => method.MethodSignature, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsImplementationAnchor(
        LoaderMethodIndex method,
        IReadOnlyList<ApiMemberReport> currentApiMembers,
        IReadOnlyList<string> keywords)
    {
        if (currentApiMembers.Any(member =>
                string.Equals(method.MethodName, member.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var methodTokens = GetSignificantTokens(method.MethodName);
        if (methodTokens.Count == 0)
        {
            return false;
        }

        foreach (var member in currentApiMembers)
        {
            var memberTokens = GetSignificantTokens(member.Name);
            if (memberTokens.Count > 0 && CountSharedTokens(methodTokens, memberTokens) > 0)
            {
                return true;
            }
        }

        var keywordTokens = GetSignificantTokens(string.Join(" ", keywords));
        return CountSharedTokens(methodTokens, keywordTokens) >= Math.Min(2, keywordTokens.Count);
    }

    private static int ScoreMethodForSurface(
        LoaderMethodIndex method,
        IReadOnlyList<string> keywords,
        IReadOnlyList<ApiMemberReport> currentApiMembers,
        IReadOnlyList<ApiImplementationReport> implementations,
        IReadOnlySet<string> anchoredExternalTypeIds)
    {
        var score = 0;
        var surfacePhrase = BuildCompactSurfaceName(keywords);
        var haystack = $"{method.TypeDisplayName} {method.MethodSignature} {string.Join(" ", method.ExternalCalls)}";
        var methodTokens = GetSignificantTokens(haystack);
        var keywordTokens = GetSignificantTokens(string.Join(" ", keywords));

        if (CountSharedTokens(methodTokens, keywordTokens) > 0)
        {
            score += 2 * CountSharedTokens(methodTokens, keywordTokens);
        }

        if (!string.IsNullOrWhiteSpace(surfacePhrase) &&
            haystack.Contains(surfacePhrase, StringComparison.OrdinalIgnoreCase))
        {
            score += 6;
        }

        foreach (var member in currentApiMembers)
        {
            var memberScore = ScoreTextRelation(method.MethodName, member.Name, keywords);
            if (memberScore >= 3)
            {
                score += memberScore;
            }
        }

        if (implementations.Any(implementation => implementation.TypeId == method.TypeId))
        {
            score += 10;
        }

        if (anchoredExternalTypeIds.Count > 0 &&
            method.ExternalCalls
                .Select(ParseExternalMethodReference)
                .Where(reference => reference is not null)
                .Cast<ExternalMethodReference>()
                .Any(reference => anchoredExternalTypeIds.Contains(reference.TypeId)))
        {
            score += 8;
        }

        return score;
    }

    private static int GetSurfaceBridgeThreshold(
        IReadOnlyList<string> keywords,
        IReadOnlyList<LoaderMethodIndex> anchoredMethods)
    {
        if (anchoredMethods.Count > 0)
        {
            return 6;
        }

        return keywords.Count <= 2 ? 3 : 4;
    }

    private static bool IsInterestingCandidateMethod(MethodCatalog method) =>
        IsInterestingLoaderMethod(method.Signature) &&
        !string.Equals(method.Name, "op_Implicit", StringComparison.Ordinal) &&
        !string.Equals(method.Name, "op_Explicit", StringComparison.Ordinal);

    private static CandidateApiOpportunityWithScore? BuildCandidateOpportunity(
        MethodCatalog method,
        IReadOnlyList<ApiMemberReport> currentApiMembers,
        IReadOnlyList<LoaderMethodIndex> assignedBridgeMethods,
        IReadOnlyList<string> keywords)
    {
        var relatedApiMembers = currentApiMembers
            .Select(member => (member, score: ScoreTextRelation(method.Name, member.Name, keywords) + ScoreTextRelation(method.Signature, member.Signature, keywords)))
            .Where(entry => entry.score >= 3)
            .OrderByDescending(entry => entry.score)
            .ThenBy(entry => entry.member.Name, StringComparer.OrdinalIgnoreCase)
            .Select(entry => entry.member.Signature)
            .Distinct(StringComparer.Ordinal)
            .Take(4)
            .ToArray();

        var relatedBridgeMethods = assignedBridgeMethods
            .Select(bridge => (bridge, score: ScoreTextRelation(method.Name, bridge.MethodName, keywords) + ScoreTextRelation(method.Signature, bridge.MethodSignature, keywords)))
            .Where(entry => entry.score >= 3)
            .OrderByDescending(entry => entry.score)
            .ThenBy(entry => entry.bridge.MethodSignature, StringComparer.OrdinalIgnoreCase)
            .Select(entry => entry.bridge.MethodSignature)
            .Distinct(StringComparer.Ordinal)
            .Take(4)
            .ToArray();

        var baseScore = ScoreCandidateMethod(method, keywords);
        var relationScore = (relatedApiMembers.Length * 3) + (relatedBridgeMethods.Length * 2);
        if (relationScore == 0 && baseScore < 5)
        {
            return null;
        }

        var suggestions = SuggestApiMembers(method, currentApiMembers, relatedApiMembers);
        if (suggestions.Count == 0)
        {
            return null;
        }

        return new CandidateApiOpportunityWithScore(
            Score: baseScore + relationScore,
            Opportunity: new CandidateApiOpportunity(
                GameMethodName: method.Name,
                GameMethodSignature: method.Signature,
                RelatedApiMembers: relatedApiMembers,
                RelatedBridgeMethods: relatedBridgeMethods,
                SuggestedApiMembers: suggestions));
    }

    private static int ScoreCandidateMethod(MethodCatalog method, IReadOnlyList<string> keywords)
    {
        var score = 0;
        var methodTokens = GetSignificantTokens(method.Signature);
        var keywordTokens = GetSignificantTokens(string.Join(" ", keywords));
        score += 2 * CountSharedTokens(methodTokens, keywordTokens);

        if (InterestingMethodNameHints.Any(hint => method.Name.Contains(hint, StringComparison.OrdinalIgnoreCase)))
        {
            score += 1;
        }

        if (method.Name.StartsWith("Get", StringComparison.Ordinal) ||
            method.Name.StartsWith("Add", StringComparison.Ordinal) ||
            method.Name.StartsWith("Set", StringComparison.Ordinal) ||
            method.Name.StartsWith("Create", StringComparison.Ordinal) ||
            method.Name.StartsWith("Try", StringComparison.Ordinal))
        {
            score += 2;
        }

        return score;
    }

    private static int ScoreTextRelation(string left, string right, IReadOnlyList<string> keywords)
    {
        var score = 0;
        var leftTokens = GetSignificantTokens(left);
        var rightTokens = GetSignificantTokens(right);
        var sharedTokens = CountSharedTokens(leftTokens, rightTokens);
        score += sharedTokens * 2;

        var keywordTokens = GetSignificantTokens(string.Join(" ", keywords));
        if (CountSharedTokens(leftTokens, keywordTokens) > 0 &&
            CountSharedTokens(rightTokens, keywordTokens) > 0)
        {
            score += 2;
        }

        return score;
    }

    private static IReadOnlyList<string> SuggestApiMembers(
        MethodCatalog method,
        IReadOnlyList<ApiMemberReport> currentApiMembers,
        IReadOnlyList<string> relatedApiMembers)
    {
        var suggestions = new List<string>();
        var methodName = method.Name;
        var parameterList = string.Join(", ", method.Parameters.Select(parameter => $"{parameter.Type} {parameter.Name}"));
        var returnType = method.ReturnType;

        AddSuggestion(BuildMethodSketch(returnType, methodName, parameterList));

        if (methodName.StartsWith("Get", StringComparison.Ordinal))
        {
            var suffix = methodName[3..];
            if (suffix.EndsWith("OrNull", StringComparison.Ordinal) && suffix.Length > 6)
            {
                var core = suffix[..^6];
                AddSuggestion(BuildMethodSketch("bool", $"TryGet{core}", AppendOutParameter(parameterList, $"{returnType}? value")));
                AddSuggestion(BuildMethodSketch($"{returnType}?", $"Get{core}", parameterList));
            }
            else if (!string.IsNullOrWhiteSpace(suffix))
            {
                AddSuggestion(BuildMethodSketch("bool", $"Try{methodName}", AppendOutParameter(parameterList, $"{returnType} value")));
            }
        }

        if (methodName.StartsWith("Add", StringComparison.Ordinal) && methodName.Length > 3)
        {
            var suffix = methodName[3..];
            AddSuggestion(BuildMethodSketch("void", $"Register{TrimPluralSuffix(suffix)}", parameterList));
            if (suffix.Length > 1)
            {
                AddSuggestion(BuildMethodSketch("void", $"RegisterMany{suffix}", parameterList));
            }
        }

        if (methodName.StartsWith("Set", StringComparison.Ordinal) ||
            methodName.StartsWith("Reveal", StringComparison.Ordinal) ||
            methodName.StartsWith("Apply", StringComparison.Ordinal) ||
            methodName.StartsWith("Can", StringComparison.Ordinal))
        {
            AddSuggestion(BuildMethodSketch(returnType, methodName, parameterList));
        }

        foreach (var related in relatedApiMembers)
        {
            var relatedName = ExtractMethodNameFromSignature(related);
            if (string.IsNullOrWhiteSpace(relatedName))
            {
                continue;
            }

            if (CountSharedTokens(GetSignificantTokens(methodName), GetSignificantTokens(relatedName)) == 0)
            {
                continue;
            }

            var suffix = DeriveSuffixFromMethodName(methodName);
            if (!string.IsNullOrWhiteSpace(suffix) &&
                !string.Equals(relatedName, methodName, StringComparison.Ordinal))
            {
                AddSuggestion(BuildMethodSketch(returnType, $"{relatedName}{suffix}", parameterList));
            }
        }

        foreach (var member in currentApiMembers)
        {
            if (member.MemberKind != "method")
            {
                continue;
            }

            if (member.Name.StartsWith("Register", StringComparison.Ordinal) &&
                methodName.StartsWith("Get", StringComparison.Ordinal))
            {
                AddSuggestion(BuildMethodSketch(returnType, $"Get{DeriveSuffixFromMethodName(methodName)}", parameterList));
            }
        }

        return suggestions
            .Distinct(StringComparer.Ordinal)
            .Take(5)
            .ToArray();

        void AddSuggestion(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion))
            {
                suggestions.Add(NormalizeWhitespace(suggestion));
            }
        }
    }

    private static string BuildMethodSketch(string returnType, string methodName, string parameterList) =>
        $"{returnType} {methodName}({parameterList})";

    private static string AppendOutParameter(string parameterList, string outParameter) =>
        string.IsNullOrWhiteSpace(parameterList)
            ? $"out {outParameter}"
            : $"{parameterList}, out {outParameter}";

    private static IReadOnlyList<string> BuildKeywords(string interfaceName)
    {
        if (string.Equals(interfaceName, "IContentRegistry", StringComparison.Ordinal))
        {
            return ["Content", "Item", "Recipe", "Icon", "Skill", "Class", "Stat", "Text", "Aggro"];
        }

        if (string.Equals(interfaceName, "IModLifecycle", StringComparison.Ordinal) ||
            string.Equals(interfaceName, "ISceneApi", StringComparison.Ordinal))
        {
            return ["Scene", "Load", "Menu", "Gameplay", "Lifecycle", "Ready"];
        }

        var trimmed = interfaceName.StartsWith("I", StringComparison.Ordinal)
            ? interfaceName[1..]
            : interfaceName;
        trimmed = trimmed
            .Replace("Api", "", StringComparison.Ordinal)
            .Replace("Registry", "", StringComparison.Ordinal)
            .Replace("Lifecycle", "Scene Lifecycle", StringComparison.Ordinal);

        return Regex.Matches(trimmed, "[A-Z][a-z0-9]*")
            .Select(match => match.Value)
            .Where(value => value.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> TokenizeName(string value) =>
        Regex.Matches(value, "[A-Z][a-z0-9]*|[a-z0-9]+")
            .Select(match => match.Value)
            .Where(token => token.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static HashSet<string> GetSignificantTokens(string value) =>
        TokenizeName(value)
            .Select(NormalizeToken)
            .Where(token => token.Length > 1)
            .Where(token => !GenericTokens.Contains(token))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static int CountSharedTokens(IReadOnlySet<string> left, IReadOnlySet<string> right)
    {
        if (left.Count == 0 || right.Count == 0)
        {
            return 0;
        }

        return left.Count(token => right.Contains(token));
    }

    private static string NormalizeToken(string token) =>
        TrimPluralSuffix(token.ToLowerInvariant());

    private static string ExtractMethodNameFromSignature(string signature)
    {
        var openParen = signature.IndexOf('(');
        if (openParen <= 0)
        {
            return "";
        }

        var prefix = signature[..openParen].Trim();
        var parts = prefix.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "" : parts[^1];
    }

    private static string DeriveSuffixFromMethodName(string methodName)
    {
        var prefixes = new[] { "TryGet", "Get", "Set", "Add", "Create", "Update", "Apply", "Reveal", "Can" };
        foreach (var prefix in prefixes.OrderByDescending(prefix => prefix.Length))
        {
            if (methodName.StartsWith(prefix, StringComparison.Ordinal) && methodName.Length > prefix.Length)
            {
                return methodName[prefix.Length..];
            }
        }

        return methodName;
    }

    private static string TrimPluralSuffix(string name)
    {
        if (name.EndsWith("ies", StringComparison.Ordinal) && name.Length > 3)
        {
            return name[..^3] + "y";
        }

        if (name.EndsWith("sses", StringComparison.Ordinal) && name.Length > 4)
        {
            return name[..^2];
        }

        if (name.EndsWith("s", StringComparison.Ordinal) && !name.EndsWith("ss", StringComparison.Ordinal) && name.Length > 1)
        {
            return name[..^1];
        }

        return name;
    }

    private static string NormalizeWhitespace(string value) =>
        Regex.Replace(value, "\\s+", " ").Trim();

    private static string BuildTitle(string interfaceName)
    {
        var trimmed = interfaceName.StartsWith("I", StringComparison.Ordinal)
            ? interfaceName[1..]
            : interfaceName;
        return string.Join(" ", Regex.Matches(trimmed, "[A-Z][a-z0-9]*").Select(match => match.Value));
    }

    private static string BuildCompactSurfaceName(IReadOnlyList<string> keywords) =>
        string.Concat(keywords.Select(keyword => keyword.Trim()));

    private static string NormalizeSlug(string value) =>
        string.Concat(value.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-'));

    private static bool IsInterestingLoaderMethod(string signature)
    {
        var ignoredNames = new[]
        {
            "Equals(",
            "GetHashCode(",
            "ToString(",
            "PrintMembers(",
            "Deconstruct(",
            ".ctor(",
            ".cctor("
        };

        return !ignoredNames.Any(name => signature.Contains(name, StringComparison.Ordinal));
    }

    private static ExternalMethodReference? ParseExternalMethodReference(string call)
    {
        var separatorIndex = call.IndexOf("::", StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return null;
        }

        var typeId = call[..separatorIndex];
        if (LoaderNamespacePrefixes.Any(prefix => typeId.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return null;
        }

        if (typeId.StartsWith("System.", StringComparison.Ordinal))
        {
            return null;
        }

        var assemblyName = typeId.StartsWith("CandideServer.", StringComparison.Ordinal)
            ? "CandideServer"
            : typeId.StartsWith("CandideCreator.Shared.", StringComparison.Ordinal)
                ? "CandideCreator.Shared"
                : typeId.StartsWith("Shared.", StringComparison.Ordinal)
                    ? "Shared"
                    : "Romestead";

        return new ExternalMethodReference(
            AssemblyName: assemblyName,
            TypeId: typeId,
            DisplayName: call);
    }

    private sealed record ExternalMethodReference(
        string AssemblyName,
        string TypeId,
        string DisplayName);

    private sealed record CandidateApiOpportunityWithScore(
        int Score,
        CandidateApiOpportunity Opportunity);

    private sealed class LoaderMethodIndex
    {
        public LoaderMethodIndex(AssemblyCatalog assembly, TypeCatalog type, MethodCatalog method)
        {
            AssemblyName = assembly.Name;
            TypeId = type.Id;
            TypeDisplayName = type.DisplayName;
            MethodId = method.Id;
            MethodName = method.Name;
            MethodSignature = method.Signature;
            Source = method.Source;
            CallReference = BuildCallReference(type.Id, method);
            LoaderCalls = method.CalledMethods
                .Where(call => LoaderNamespacePrefixes.Any(prefix => call.StartsWith(prefix, StringComparison.Ordinal)))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            ExternalCalls = method.CalledMethods
                .Where(call => !LoaderNamespacePrefixes.Any(prefix => call.StartsWith(prefix, StringComparison.Ordinal)))
                .Where(call => !call.StartsWith("System.", StringComparison.Ordinal))
                .Where(call => !call.StartsWith("Microsoft.", StringComparison.Ordinal))
                .Where(call => !call.StartsWith("HarmonyLib.", StringComparison.Ordinal))
                .Where(call => !call.StartsWith("MonoGame.", StringComparison.Ordinal))
                .Where(call => !call.StartsWith("Dictionary.", StringComparison.Ordinal))
                .Where(call => !call.StartsWith("List.", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        public string AssemblyName { get; }
        public string TypeId { get; }
        public string TypeDisplayName { get; }
        public string MethodId { get; }
        public string MethodName { get; }
        public string MethodSignature { get; }
        public SourceLocation? Source { get; }
        public string CallReference { get; }
        public IReadOnlyList<string> LoaderCalls { get; }
        public IReadOnlyList<string> ExternalCalls { get; }

        private static string BuildCallReference(string typeId, MethodCatalog method)
        {
            var genericSuffix = method.GenericArity > 0 ? $"`{method.GenericArity}" : "";
            var parameterTypes = string.Join(", ", method.Parameters.Select(parameter => parameter.Type));
            return $"{typeId}::{method.Name}{genericSuffix}({parameterTypes})";
        }
    }
}
