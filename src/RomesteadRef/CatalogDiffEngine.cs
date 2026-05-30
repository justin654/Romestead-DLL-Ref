namespace RomesteadRef;

internal static class CatalogDiffEngine
{
    public static CatalogDiff Compare(CatalogSnapshot oldSnapshot, CatalogSnapshot newSnapshot)
    {
        var assemblyChanges = CompareAssemblies(oldSnapshot, newSnapshot);
        var typeChanges = CompareTypes(oldSnapshot, newSnapshot);
        var methodChanges = CompareMembers(
            oldSnapshot,
            newSnapshot,
            type => type.Methods.Select(method => (method.Id, method.Signature, method.Hash)));
        var propertyChanges = CompareMembers(
            oldSnapshot,
            newSnapshot,
            type => type.Properties.Select(property => (property.Id, property.Signature, property.Hash)));
        var fieldChanges = CompareMembers(
            oldSnapshot,
            newSnapshot,
            type => type.Fields.Select(field => (field.Id, field.Signature, field.Hash)));

        return new CatalogDiff(
            Summary: new DiffSummary(
                AddedAssemblies: assemblyChanges.Count(change => change.ChangeKind == "added"),
                RemovedAssemblies: assemblyChanges.Count(change => change.ChangeKind == "removed"),
                ChangedAssemblies: assemblyChanges.Count(change => change.ChangeKind == "changed"),
                AddedTypes: typeChanges.Count(change => change.ChangeKind == "added"),
                RemovedTypes: typeChanges.Count(change => change.ChangeKind == "removed"),
                ChangedTypes: typeChanges.Count(change => change.ChangeKind == "changed"),
                AddedMethods: methodChanges.Count(change => change.ChangeKind == "added"),
                RemovedMethods: methodChanges.Count(change => change.ChangeKind == "removed"),
                ChangedMethods: methodChanges.Count(change => change.ChangeKind == "changed"),
                AddedProperties: propertyChanges.Count(change => change.ChangeKind == "added"),
                RemovedProperties: propertyChanges.Count(change => change.ChangeKind == "removed"),
                ChangedProperties: propertyChanges.Count(change => change.ChangeKind == "changed"),
                AddedFields: fieldChanges.Count(change => change.ChangeKind == "added"),
                RemovedFields: fieldChanges.Count(change => change.ChangeKind == "removed"),
                ChangedFields: fieldChanges.Count(change => change.ChangeKind == "changed")),
            AssemblyChanges: assemblyChanges,
            TypeChanges: typeChanges,
            MethodChanges: methodChanges,
            PropertyChanges: propertyChanges,
            FieldChanges: fieldChanges);
    }

    private static IReadOnlyList<AssemblyChange> CompareAssemblies(CatalogSnapshot oldSnapshot, CatalogSnapshot newSnapshot)
    {
        var oldAssemblies = oldSnapshot.Assemblies.ToDictionary(assembly => assembly.Name, StringComparer.OrdinalIgnoreCase);
        var newAssemblies = newSnapshot.Assemblies.ToDictionary(assembly => assembly.Name, StringComparer.OrdinalIgnoreCase);
        var names = oldAssemblies.Keys.Union(newAssemblies.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

        var changes = new List<AssemblyChange>();
        foreach (var name in names)
        {
            var hasOld = oldAssemblies.TryGetValue(name, out var oldAssembly);
            var hasNew = newAssemblies.TryGetValue(name, out var newAssembly);

            if (!hasOld && hasNew)
            {
                changes.Add(new AssemblyChange("added", name, null, newAssembly!.Version, null, newAssembly.Hash));
                continue;
            }

            if (hasOld && !hasNew)
            {
                changes.Add(new AssemblyChange("removed", name, oldAssembly!.Version, null, oldAssembly.Hash, null));
                continue;
            }

            if (oldAssembly!.Hash != newAssembly!.Hash || oldAssembly.Version != newAssembly.Version)
            {
                changes.Add(new AssemblyChange("changed", name, oldAssembly.Version, newAssembly.Version, oldAssembly.Hash, newAssembly.Hash));
            }
        }

        return changes;
    }

    private static IReadOnlyList<TypeChange> CompareTypes(CatalogSnapshot oldSnapshot, CatalogSnapshot newSnapshot)
    {
        var oldTypes = BuildUniqueMap(
            oldSnapshot.Assemblies.SelectMany(assembly => assembly.Types.Select(type => ($"{assembly.Name}::{type.Id}", (assembly.Name, type)))));
        var newTypes = BuildUniqueMap(
            newSnapshot.Assemblies.SelectMany(assembly => assembly.Types.Select(type => ($"{assembly.Name}::{type.Id}", (assembly.Name, type)))));
        var keys = oldTypes.Keys.Union(newTypes.Keys, StringComparer.Ordinal).OrderBy(key => key, StringComparer.OrdinalIgnoreCase);

        var changes = new List<TypeChange>();
        foreach (var key in keys)
        {
            var hasOld = oldTypes.TryGetValue(key, out var oldType);
            var hasNew = newTypes.TryGetValue(key, out var newType);
            var assemblyName = key[..key.IndexOf("::", StringComparison.Ordinal)];
            var typeId = key[(key.IndexOf("::", StringComparison.Ordinal) + 2)..];

            if (!hasOld && hasNew)
            {
                changes.Add(new TypeChange("added", assemblyName, typeId, newType!.type.DisplayName, null, newType.type.Hash));
                continue;
            }

            if (hasOld && !hasNew)
            {
                changes.Add(new TypeChange("removed", assemblyName, typeId, oldType!.type.DisplayName, oldType.type.Hash, null));
                continue;
            }

            if (oldType!.type.Hash != newType!.type.Hash)
            {
                changes.Add(new TypeChange("changed", assemblyName, typeId, newType.type.DisplayName, oldType.type.Hash, newType.type.Hash));
            }
        }

        return changes;
    }

    private static IReadOnlyList<MemberChange> CompareMembers(
        CatalogSnapshot oldSnapshot,
        CatalogSnapshot newSnapshot,
        Func<TypeCatalog, IEnumerable<(string Id, string DisplayName, string Hash)>> selector)
    {
        var oldMembers = BuildUniqueMap(
            oldSnapshot.Assemblies.SelectMany(assembly => assembly.Types.SelectMany(type =>
                selector(type).Select(member => ($"{assembly.Name}::{type.Id}::{member.Id}", (assembly.Name, type.Id, member))))));
        var newMembers = BuildUniqueMap(
            newSnapshot.Assemblies.SelectMany(assembly => assembly.Types.SelectMany(type =>
                selector(type).Select(member => ($"{assembly.Name}::{type.Id}::{member.Id}", (assembly.Name, type.Id, member))))));
        var keys = oldMembers.Keys.Union(newMembers.Keys, StringComparer.Ordinal).OrderBy(key => key, StringComparer.OrdinalIgnoreCase);

        var changes = new List<MemberChange>();
        foreach (var key in keys)
        {
            var hasOld = oldMembers.TryGetValue(key, out var oldMember);
            var hasNew = newMembers.TryGetValue(key, out var newMember);
            var split = key.Split(new[] { "::" }, 3, StringSplitOptions.None);
            var assemblyName = split[0];
            var typeId = split[1];
            var memberId = split[2];

            if (!hasOld && hasNew)
            {
                changes.Add(new MemberChange("added", assemblyName, typeId, memberId, newMember!.member.DisplayName, null, newMember.member.Hash));
                continue;
            }

            if (hasOld && !hasNew)
            {
                changes.Add(new MemberChange("removed", assemblyName, typeId, memberId, oldMember!.member.DisplayName, oldMember.member.Hash, null));
                continue;
            }

            if (oldMember!.member.Hash != newMember!.member.Hash)
            {
                changes.Add(new MemberChange("changed", assemblyName, typeId, memberId, newMember.member.DisplayName, oldMember.member.Hash, newMember.member.Hash));
            }
        }

        return changes;
    }

    private static Dictionary<string, TValue> BuildUniqueMap<TValue>(IEnumerable<(string Key, TValue Value)> entries)
    {
        var map = new Dictionary<string, TValue>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (!map.ContainsKey(entry.Key))
            {
                map.Add(entry.Key, entry.Value);
            }
        }

        return map;
    }
}
