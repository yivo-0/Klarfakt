/// <summary>
/// The arguments after the command name.
/// </summary>
/// <remarks>
/// Every option is named here, and anything starting with '-' that is not one is refused. It used
/// to be kept: a mistyped --stict went into the flag set, nothing read it, and the run reported
/// exit 0 and "valid" for an archive nobody had validated strictly. A tool whose whole job is
/// saying when something is wrong should not begin by ignoring what it was asked to do.
/// </remarks>
internal sealed class CommandLine
{
    /// <summary>Options that take the next argument as their value.</summary>
    internal static readonly string[] ValueOptions =
        ["--rules", "--csv", "--out", "-o", "--lang", "--parallel"];

    /// <summary>Options that stand alone.</summary>
    internal static readonly string[] FlagOptions =
        ["--recursive", "-r", "--json", "--no-schema", "--strict", "--force", "--help", "-h"];

    private CommandLine(List<string> paths, HashSet<string> flags, Dictionary<string, string> values, string? problem)
    {
        Paths = paths;
        Flags = flags;
        Values = values;
        Problem = problem;
    }

    internal List<string> Paths { get; }

    internal HashSet<string> Flags { get; }

    internal Dictionary<string, string> Values { get; }

    /// <summary>What was wrong with the arguments, or null when they parsed.</summary>
    internal string? Problem { get; }

    internal bool WantsHelp => Flags.Contains("--help") || Flags.Contains("-h");

    internal static CommandLine Parse(IReadOnlyList<string> arguments)
    {
        var paths = new List<string>();
        var flags = new HashSet<string>(StringComparer.Ordinal);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];

            if (!argument.StartsWith('-'))
            {
                paths.Add(argument);
                continue;
            }

            // --rules=xrechnung as well as --rules xrechnung: both forms turn up in scripts.
            if (argument.IndexOf('=') is var equals && equals > 0)
            {
                var name = argument[..equals];
                if (!ValueOptions.Contains(name, StringComparer.Ordinal))
                {
                    return Failed(paths, flags, values, Unknown(name));
                }

                values[name] = argument[(equals + 1)..];
                continue;
            }

            // Consumed with their argument, otherwise "--rules xrechnung" would treat "xrechnung"
            // as a file to validate.
            if (ValueOptions.Contains(argument, StringComparer.Ordinal))
            {
                if (index + 1 >= arguments.Count)
                {
                    return Failed(paths, flags, values, $"'{argument}' needs a value.");
                }

                values[argument] = arguments[++index];
                continue;
            }

            if (FlagOptions.Contains(argument, StringComparer.Ordinal))
            {
                flags.Add(argument);
                continue;
            }

            return Failed(paths, flags, values, Unknown(argument));
        }

        return new CommandLine(paths, flags, values, null);
    }

    private static string Unknown(string option)
    {
        var suggestion = FlagOptions.Concat(ValueOptions)
            .Where(known => Distance(known, option) <= 2)
            .OrderBy(known => Distance(known, option))
            .FirstOrDefault();

        return $"Unknown option '{option}'."
               + (suggestion is null ? string.Empty : $" Did you mean '{suggestion}'?");
    }

    private static CommandLine Failed(
        List<string> paths, HashSet<string> flags, Dictionary<string, string> values, string problem) =>
        new(paths, flags, values, problem);

    /// <summary>Levenshtein distance, only ever run once on a handful of short strings.</summary>
    private static int Distance(string left, string right)
    {
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var column = 0; column <= right.Length; column++) previous[column] = column;

        for (var row = 1; row <= left.Length; row++)
        {
            current[0] = row;

            for (var column = 1; column <= right.Length; column++)
            {
                var substitution = previous[column - 1] + (left[row - 1] == right[column - 1] ? 0 : 1);
                current[column] = Math.Min(Math.Min(current[column - 1] + 1, previous[column] + 1), substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }
}
