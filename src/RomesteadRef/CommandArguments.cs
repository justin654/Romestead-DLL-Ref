namespace RomesteadRef;

internal sealed class CommandArguments
{
    private readonly Dictionary<string, List<string>> _options = new(StringComparer.OrdinalIgnoreCase);

    private CommandArguments(string name, List<string> positionals)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "scan" : name;
        Positionals = positionals;
    }

    public string Name { get; }
    public List<string> Positionals { get; }

    public bool HasFlag(string name) => _options.ContainsKey(name);

    public string? GetSingle(string name) =>
        _options.TryGetValue(name, out var values) && values.Count > 0 ? values[^1] : null;

    public List<string> GetMany(string name) =>
        _options.TryGetValue(name, out var values) ? [.. values] : [];

    public static CommandArguments Parse(string[] args)
    {
        var command = "scan";
        var index = 0;

        if (args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal))
        {
            command = args[0];
            index = 1;
        }

        var parsed = new CommandArguments(command, []);
        while (index < args.Length)
        {
            var current = args[index];
            if (current.StartsWith("--", StringComparison.Ordinal))
            {
                if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    parsed.AddOption(current, args[index + 1]);
                    index += 2;
                    continue;
                }

                parsed.AddOption(current, "true");
                index++;
                continue;
            }

            parsed.Positionals.Add(current);
            index++;
        }

        return parsed;
    }

    private void AddOption(string name, string value)
    {
        if (!_options.TryGetValue(name, out var list))
        {
            list = [];
            _options[name] = list;
        }

        list.Add(value);
    }
}
