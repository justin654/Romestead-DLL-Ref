namespace RomesteadRef;

public sealed record ApiOpportunityReport(
    DateTime GeneratedAtUtc,
    IReadOnlyList<ApiSurfaceReport> Surfaces);

public sealed record ApiSurfaceReport(
    string DomainId,
    string Title,
    string InterfaceTypeId,
    string InterfaceDisplayName,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<ApiMemberReport> CurrentApiMembers,
    IReadOnlyList<ApiImplementationReport> Implementations,
    IReadOnlyList<LoaderBridgeMethodReport> BridgeMethods,
    IReadOnlyList<ExternalTypeOpportunity> ExternalTypes);

public sealed record ApiMemberReport(
    string MemberKind,
    string Name,
    string Signature);

public sealed record ApiImplementationReport(
    string AssemblyName,
    string TypeId,
    string DisplayName);

public sealed record LoaderBridgeMethodReport(
    string AssemblyName,
    string TypeId,
    string TypeDisplayName,
    string MethodId,
    string MethodSignature,
    SourceLocation? Source,
    IReadOnlyList<string> ExternalCalls);

public sealed record ExternalTypeOpportunity(
    string AssemblyName,
    string TypeId,
    string DisplayName,
    IReadOnlyList<string> AlreadyTouchedMethods,
    IReadOnlyList<CandidateApiOpportunity> Candidates);

public sealed record CandidateApiOpportunity(
    string GameMethodName,
    string GameMethodSignature,
    IReadOnlyList<string> RelatedApiMembers,
    IReadOnlyList<string> RelatedBridgeMethods,
    IReadOnlyList<string> SuggestedApiMembers);
