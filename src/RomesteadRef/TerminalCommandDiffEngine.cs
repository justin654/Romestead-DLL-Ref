namespace RomesteadRef;

internal static class TerminalCommandDiffEngine
{
    public static TerminalCommandDiff? Compare(TerminalCommandCatalog? oldCatalog, TerminalCommandCatalog newCatalog)
    {
        if (oldCatalog is null)
        {
            return null;
        }

        var oldByName = oldCatalog.Commands.ToDictionary(command => command.Name, StringComparer.Ordinal);
        var newByName = newCatalog.Commands.ToDictionary(command => command.Name, StringComparer.Ordinal);

        var added = newByName
            .Where(entry => !oldByName.ContainsKey(entry.Key))
            .Select(entry => entry.Value)
            .OrderBy(command => command.Name, StringComparer.Ordinal)
            .ToArray();
        var removed = oldByName
            .Where(entry => !newByName.ContainsKey(entry.Key))
            .Select(entry => entry.Value)
            .OrderBy(command => command.Name, StringComparer.Ordinal)
            .ToArray();
        var changed = newByName
            .Where(entry => oldByName.TryGetValue(entry.Key, out var oldCommand) &&
                            !string.Equals(oldCommand.ChangeHash, entry.Value.ChangeHash, StringComparison.Ordinal))
            .Select(entry => new TerminalCommandChange(entry.Key, oldByName[entry.Key], entry.Value))
            .OrderBy(change => change.Name, StringComparer.Ordinal)
            .ToArray();

        return new TerminalCommandDiff(
            new TerminalCommandDiffSummary(added.Length, removed.Length, changed.Length),
            added,
            removed,
            changed,
            oldCatalog.Metadata.GeneratedAtUtc,
            newCatalog.Metadata.GeneratedAtUtc);
    }
}
